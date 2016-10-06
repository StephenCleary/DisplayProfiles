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
            ApplicationSettingsFilename = Path.Combine(ProfileFiles.SettingsDirectory, "settings.json");
        }

        public static string ApplicationSettingsFilename { get; }

        public static void EnsureCreated()
        {
            ProfileFiles.EnsureCreated();
            try
            {
                ApplicationSettings = JsonConvert.DeserializeObject<ApplicationSettings>(File.ReadAllText(ApplicationSettingsFilename), Profile.JsonSerializerSettings);
            }
            catch
            {
                ApplicationSettings = new ApplicationSettings();
            }
        }

        public static ApplicationSettings ApplicationSettings { get; private set; }

        public static void SaveApplicationSettings()
        {
            File.WriteAllText(ApplicationSettingsFilename, JsonConvert.SerializeObject(ApplicationSettings, Profile.JsonSerializerSettings));
        }
    }
}
