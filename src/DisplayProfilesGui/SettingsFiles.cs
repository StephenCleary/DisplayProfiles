using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DisplayProfiles;
using Newtonsoft.Json;

namespace DisplayProfilesGui
{
    public static class SettingsFiles
    {
        static SettingsFiles()
        {
            SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DisplayProfiles");
            SettingsProfilesDirectory = Path.Combine(SettingsDirectory, "Profiles");
            ApplicationSettingsFilename = Path.Combine(SettingsDirectory, "settings.json");
        }

        public static string SettingsDirectory { get; }
        private static string SettingsProfilesDirectory { get; }
        private static string ApplicationSettingsFilename { get; }

        public static void EnsureCreated()
        {
            if (!Directory.Exists(SettingsDirectory))
                Directory.CreateDirectory(SettingsDirectory);
            if (!Directory.Exists(SettingsProfilesDirectory))
                Directory.CreateDirectory(SettingsProfilesDirectory);
            try
            {
                ApplicationSettings = JsonConvert.DeserializeObject<ApplicationSettings>(File.ReadAllText(ApplicationSettingsFilename), Profile.JsonSerializerSettings);
            }
            catch
            {
                ApplicationSettings = new ApplicationSettings();
            }
        }

        public static string ProfileNameToFileName(string profileName)
        {
            return Path.Combine(SettingsProfilesDirectory, profileName + ".json");
        }

        public static string FileNameToProfileName(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName);
        }

        public static List<string> GetProfileNames()
        {
            return Directory.GetFiles(SettingsProfilesDirectory, "*.json").Select(FileNameToProfileName).OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase).ToList();
        }

        public static ApplicationSettings ApplicationSettings { get; private set; }

        public static void SaveApplicationSettings()
        {
            File.WriteAllText(ApplicationSettingsFilename, JsonConvert.SerializeObject(ApplicationSettings, Profile.JsonSerializerSettings));
        }
    }
}
