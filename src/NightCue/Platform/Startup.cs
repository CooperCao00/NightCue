using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NightCue
{
    public interface IStartupStore
    {
        string Read();
        void Write(string command);
    }

    public sealed class RegistryStartupStore : IStartupStore
    {
        const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Name = "NightCue";
        public string Read()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Key))
                return key == null ? null : key.GetValue(Name) as string;
        }
        public void Write(string command)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(Key))
            {
                if (command == null) key.DeleteValue(Name, false);
                else key.SetValue(Name, command, RegistryValueKind.String);
                key.Flush();
            }
        }
    }

    public enum StartupRegistration { Unregistered, CurrentPath, DifferentPath, Unavailable }

    public static class Startup
    {
        static readonly IStartupStore registry = new RegistryStartupStore();
        public static string BuildCommand(string executable)
        {
            if (String.IsNullOrWhiteSpace(executable) || executable.IndexOf('"') >= 0)
                throw new ArgumentException("Invalid executable path.", "executable");
            string command = "\"" + executable + "\" --background";
            return command;
        }
        public static string Command { get { return BuildCommand(Application.ExecutablePath); } }
        // Registration is not Windows approval or proof of a successful sign-in launch.
        public static StartupRegistration Registration { get { return Inspect(registry, Command); } }
        public static StartupRegistration Inspect(IStartupStore store, string command)
        {
            try
            {
                string value = store.Read();
                return String.IsNullOrEmpty(value) ? StartupRegistration.Unregistered :
                    String.Equals(value, command, StringComparison.OrdinalIgnoreCase) ?
                    StartupRegistration.CurrentPath : StartupRegistration.DifferentPath;
            }
            catch (Exception e)
            {
                Store.Log(e);
                return StartupRegistration.Unavailable;
            }
        }
        public static bool Registered { get { return !String.IsNullOrEmpty(registry.Read()); } }
        public static bool Enabled { get { return String.Equals(registry.Read(), Command, StringComparison.OrdinalIgnoreCase); } }
        public static bool NeedsRepair { get { return Registered && !Enabled; } }
        public static void Set(bool enabled) { SaveSettings(enabled, delegate { }); }

        public static void SaveSettings(bool enabled, Action save)
        {
            SaveSettings(registry, enabled ? Command : null, save);
        }
        public static void SaveSettings(IStartupStore store, string command, Action save)
        {
            if (command != null && command.Length > 260) throw new ArgumentException("Startup command exceeds the Windows Run limit.", "command");
            string previous = store.Read();
            try
            {
                store.Write(command);
                if (!String.Equals(store.Read(), command, StringComparison.Ordinal))
                    throw new InvalidOperationException("Startup registration could not be verified.");
                save();
            }
            catch (Exception failure)
            {
                // A moved app must restore the actual old path, not just a boolean.
                try { store.Write(previous); }
                catch (Exception rollback) { throw new AggregateException("Startup rollback failed.", failure, rollback); }
                throw;
            }
        }
    }
}
