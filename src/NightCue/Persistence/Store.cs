using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NightCue
{
    public static class Store
    {
        const long MaxLogBytes = 131072;
        const int MaxEntryCharacters = 16384;
        const string RecoveryMarker = "recovery.pending";
        static readonly object gate = new object();
        static readonly HashSet<string> protectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> pendingDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly Encoding logEncoding = new UTF8Encoding(false);
        public static string Root = AppDomain.CurrentDomain.BaseDirectory;
        // Injectable so tests never touch the current user's real diagnostic files.
        public static string DiagnosticsRoot = DefaultDiagnosticDirectory();
        public static string DiagnosticDirectory
        {
            get
            {
                try { return String.IsNullOrEmpty(DiagnosticsRoot) ? "" : Path.GetFullPath(DiagnosticsRoot); }
                catch { return ""; }
            }
        }

        static string DefaultDiagnosticDirectory()
        {
            try
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return String.IsNullOrWhiteSpace(local) ? "" : Path.Combine(local, "NightCue", "Diagnostics");
            }
            catch { return ""; }
        }

        public static bool RecoveryNotice
        {
            get
            {
                lock (gate)
                {
                    string directory = DiagnosticDirectory;
                    return pendingDirectories.Contains(directory) || (directory.Length > 0 && File.Exists(Path.Combine(directory, RecoveryMarker)));
                }
            }
        }

        public static string PathFor(string name)
        {
            return Path.Combine(Root, name);
        }

        public static T Read<T>(string name)
            where T : new()
        {
            string path = PathFor(name);
            // Configuration is local data: never resolve external entities or DTDs.
            XmlReaderSettings settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1048576
            };
            try
            {
                using (FileStream f = File.OpenRead(path))
                using (XmlReader reader = XmlReader.Create(f, settings))
                {
                    object value = new XmlSerializer(typeof(T)).Deserialize(reader);
                    return value == null ? new T() : (T)value;
                }
            }
            // File.Exists hides permission errors; only a genuinely absent file uses defaults here.
            catch (FileNotFoundException) { return new T(); }
            catch (DirectoryNotFoundException) { return new T(); }
        }

        public static T ReadRecovering<T>(string name, Action<T> validate) where T : new()
        {
            T value;
            try { value = Read<T>(name); }
            catch (Exception failure)
            {
                if (IsStorageFailure(failure)) return Recover<T>(name, validate, failure, false);
                if (IsDataFailure(failure)) return Recover<T>(name, validate, failure, true);
                throw;
            }
            if (validate != null)
            {
                try { validate(value); }
                catch (Exception failure)
                {
                    if (!IsDataFailure(failure)) throw;
                    return Recover<T>(name, validate, failure, true);
                }
            }
            return value;
        }

        static bool IsStorageFailure(Exception failure)
        {
            for (Exception current = failure; current != null; current = current.InnerException)
                if (current is IOException || current is UnauthorizedAccessException || current is SecurityException)
                    return true;
            return false;
        }

        static bool IsDataFailure(Exception failure)
        {
            return failure is XmlException || failure is InvalidOperationException || failure is ArgumentException ||
                failure is FormatException || failure is OverflowException;
        }

        static T Recover<T>(string name, Action<T> validate, Exception failure, bool canBackUp) where T : new()
        {
            T defaults = new T();
            if (validate != null) validate(defaults);
            string original = Path.GetFullPath(PathFor(name));
            MarkRecovery(name);
            Log(failure);
            if (!canBackUp)
            {
                Protect(original);
                Diagnostics("configuration-read-failed", "file=" + original + "; using in-memory defaults; original preserved; restore access and restart to retry");
                return defaults;
            }

            string backup;
            try
            {
                Directory.CreateDirectory(DiagnosticDirectory);
                backup = Path.Combine(DiagnosticDirectory, Path.GetFileName(name) + ".corrupt-" +
                    DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N") + ".bak");
                File.Copy(original, backup, false);
            }
            catch (Exception backupFailure)
            {
                Protect(original);
                Log(backupFailure);
                Diagnostics("configuration-backup-failed", "file=" + original + "; using in-memory defaults; saving blocked to preserve original; restore access and restart to retry");
                return defaults;
            }

            Diagnostics("configuration-backed-up", "file=" + original + "; backup=" + backup);
            try
            {
                Save(name, defaults);
                Diagnostics("configuration-restored", "file=" + original + "; defaults saved");
            }
            catch (Exception saveFailure)
            {
                Log(saveFailure);
                Diagnostics("configuration-restore-save-failed", "file=" + original + "; backup=" + backup + "; using in-memory defaults; original preserved");
            }
            return defaults;
        }

        static void Protect(string path)
        {
            lock (gate) protectedFiles.Add(path);
        }

        static void MarkRecovery(string name)
        {
            try
            {
                lock (gate)
                {
                    string directory = DiagnosticDirectory;
                    pendingDirectories.Add(directory);
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, RecoveryMarker), DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " " + name, logEncoding);
                }
            }
            catch (Exception failure) { Log(failure); }
        }

        public static void AcknowledgeRecovery()
        {
            try
            {
                lock (gate)
                {
                    string directory = DiagnosticDirectory;
                    if (directory.Length > 0) File.Delete(Path.Combine(directory, RecoveryMarker));
                    pendingDirectories.Remove(directory);
                }
                Diagnostics("configuration-recovery-acknowledged", "Recovery notice dismissed; backups retained");
            }
            catch (Exception failure) { Log(failure); }
        }

        public static void Save<T>(string name, T value)
        {
            string path = PathFor(name), temp = path + ".tmp";
            lock (gate)
                if (protectedFiles.Contains(Path.GetFullPath(path)))
                    throw new IOException("Saving is blocked because the original configuration could not be safely read or backed up. Restore access and restart NightCue to retry: " + path);
            using (FileStream f = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new XmlSerializer(typeof(T)).Serialize(f, value);
                f.Flush(true);
            }

            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        public static void Log(Exception e)
        {
            AppendLog("error.log", "error", e == null ? "Unknown error" : e.ToString());
        }

        public static void Diagnostics(string eventName, string detail)
        {
            AppendLog("diagnostics.log", eventName, detail);
        }

        static void AppendLog(string name, string eventName, string detail)
        {
            try
            {
                string entry = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " [" + eventName + "] " + detail;
                if (entry.Length > MaxEntryCharacters) entry = entry.Substring(0, MaxEntryCharacters) + " [truncated]";
                entry = entry.Replace("\r", "\\r").Replace("\n", "\\n") + Environment.NewLine;
                byte[] bytes = logEncoding.GetBytes(entry);
                lock (gate)
                {
                    string directory = DiagnosticDirectory;
                    Directory.CreateDirectory(directory);
                    string path = Path.Combine(directory, name);
                    if (File.Exists(path) && new FileInfo(path).Length + bytes.Length > MaxLogBytes)
                    {
                        string previous = path + ".1";
                        if (File.Exists(previous)) File.Delete(previous);
                        File.Move(path, previous);
                    }
                    using (FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                        stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch
            {
                // Diagnostics must never prevent a tray application from starting.
            }
        }
    }
}
