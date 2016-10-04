using System;
using System.IO;
using System.Reflection;
using DisplayProfiles.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DisplayProfiles
{
    public static class Profile
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        public static DisplaySettings LoadDisplaySettings(string filename)
        {
            return JsonConvert.DeserializeObject<DisplaySettings>(File.ReadAllText(filename), JsonSerializerSettings)
                .UpdateAdapterIds();
        }

        public static void SaveCurrentDisplaySettings(string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(DisplaySettings.GetCurrent(activeOnly: true), JsonSerializerSettings));
        }
    }
}
