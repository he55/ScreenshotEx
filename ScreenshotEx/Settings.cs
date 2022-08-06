using System;
using System.IO;
using System.Xml.Serialization;

namespace ScreenshotEx
{
    public class Settings
    {
        static string settingsFilePath = Helper.GetPathForUserAppDataFolder("settings.xml");
        static Settings s_settings;

        private Settings() { }

        public bool IsFirstRun { get; set; } = true;
        public string SavePath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public int SaveName { get; set; }
        public int SaveExtension { get; set; }
        public int OpenApp { get; set; }
        public bool IsShowPreview { get; set; } = true;
        public bool IsPlaySound { get; set; } = true;
        public bool UseHotkey { get; set; }

        public static Settings Load()
        {
            if (s_settings == null)
            {
                if (File.Exists(settingsFilePath))
                {
                    using (FileStream fileStream = File.OpenRead(settingsFilePath))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
                        s_settings = (Settings)xmlSerializer.Deserialize(fileStream);
                    }
                }
                else
                {
                    s_settings = new Settings();
                }
            }
            return s_settings;
        }

        public static void Save()
        {
            using (FileStream fileStream = File.Create(settingsFilePath))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
                xmlSerializer.Serialize(fileStream, s_settings);
            }
        }
    }
}
