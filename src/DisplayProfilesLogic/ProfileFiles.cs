using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DisplayProfiles
{
    public static class ProfileFiles
    {
        static ProfileFiles()
        {
            SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DisplayProfiles");
            SettingsProfilesDirectory = Path.Combine(SettingsDirectory, "Profiles");
        }

        public static string SettingsDirectory { get; }
        public static string SettingsProfilesDirectory { get; }

        public static void EnsureCreated()
        {
            if (!Directory.Exists(SettingsDirectory))
                Directory.CreateDirectory(SettingsDirectory);
            if (!Directory.Exists(SettingsProfilesDirectory))
                Directory.CreateDirectory(SettingsProfilesDirectory);
        }

        private static string ProfileNameToFileName(string profileName) => Path.Combine(SettingsProfilesDirectory, profileName + ".json");

        private static string FileNameToProfileName(string fileName) => Path.GetFileNameWithoutExtension(fileName);

        public static List<string> GetProfileNames()
        {
            return Directory.GetFiles(SettingsProfilesDirectory, "*.json").Select(FileNameToProfileName).OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase).ToList();
        }

        public static DisplaySettings LoadRawProfile(string name) => Profile.LoadDisplaySettings(ProfileNameToFileName(name));
        public static DisplaySettings LoadProfile(string name) => LoadRawProfile(name).UpdateAdapterIds();
        public static void SaveProfile(string name) => Profile.SaveCurrentDisplaySettings(ProfileNameToFileName(name));
        public static void DeleteProfile(string name) => File.Delete(ProfileNameToFileName(name));
    }
}
