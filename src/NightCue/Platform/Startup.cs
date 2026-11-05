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

    public static class Startup
    {
        static readonly IStartupStore registry = new RegistryStartupStore();
        public static string BuildCommand(string executable)
        {
            if (String.IsNullOrWhiteSpace(executable) || executable.IndexOf('"') >= 0)
                throw new ArgumentException("Invalid executable path.", "executable");
            return "\"" + executable + "\" --background";
        }
        public static string Command { get { return BuildCommand(Application.ExecutablePath); } }
        public static bool Registered { get { return !String.IsNullOrEmpty(registry.Read()); } }
        public static bool Enabled { get { return String.Equals(registry.Read(), Command, StringComparison.OrdinalIgnoreCase); } }
        public static bool NeedsRepair { get { return Registered && !Enabled; } }
        public static void Set(bool enabled) { registry.Write(enabled ? Command : null); }

        public static void SaveSettings(bool enabled, Action save)
        {
            SaveSettings(registry, enabled ? Command : null, save);
        }
        public static void SaveSettings(IStartupStore store, string command, Action save)
        {
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
