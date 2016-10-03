using System.IO;
using System.Reflection;
using DisplayProfiles.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DisplayProfiles
{
    public static class Profile
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { ContractResolver = DisplayConfigModeInfoContractResolver.Instance };

        public static void LoadDisplaySettingsAndSetAsCurrent(string filename)
        {
            JsonConvert.DeserializeObject<DisplaySettings>(File.ReadAllText(filename), JsonSerializerSettings)
                .UpdateAdapterIds()
                .SetCurrent();
        }

        public static void SaveCurrentDisplaySettings(string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(DisplaySettings.GetCurrent(activeOnly: true), JsonSerializerSettings));
        }

        private sealed class DisplayConfigModeInfoContractResolver : DefaultContractResolver
        {
            public static readonly DisplayConfigModeInfoContractResolver Instance = new DisplayConfigModeInfoContractResolver();

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                if (property.DeclaringType == typeof(Ccd.DisplayConfigModeInfo))
                {
                    if (property.PropertyName == "targetMode")
                    {
                        property.ShouldSerialize = obj =>
                        {
                            var instance = (Ccd.DisplayConfigModeInfo)obj;
                            return instance.infoType == Ccd.DisplayConfigModeInfoType.Target;
                        };
                    }
                    else if (property.PropertyName == "sourceMode")
                    {
                        property.ShouldSerialize = obj =>
                        {
                            var instance = (Ccd.DisplayConfigModeInfo)obj;
                            return instance.infoType == Ccd.DisplayConfigModeInfoType.Source;
                        };
                    }
                }

                return property;
            }
        }

    }
}
