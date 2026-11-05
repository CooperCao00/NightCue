using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace NightCue
{
    public static class Store
    {
        public static string Root = AppDomain.CurrentDomain.BaseDirectory;
        public static string PathFor(string name)
        {
            return Path.Combine(Root, name);
        }

        public static T Read<T>(string name)
            where T : new()
        {
            string path = PathFor(name);
            if (!File.Exists(path))
                return new T();
            // Configuration is local data: never resolve external entities or DTDs.
            XmlReaderSettings settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1048576
            };
            using (FileStream f = File.OpenRead(path))
            using (XmlReader reader = XmlReader.Create(f, settings))
            {
                object value = new XmlSerializer(typeof(T)).Deserialize(reader);
                return value == null ? new T() : (T)value;
            }
        }

        public static void Save<T>(string name, T value)
        {
            string path = PathFor(name), temp = path + ".tmp";
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
            try
            {
                string p = PathFor("error.log");
                if (File.Exists(p) && new FileInfo(p).Length > 131072)
                    File.WriteAllText(p, "");
                File.AppendAllText(p, DateTime.Now.ToString("s") + " " + e + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
