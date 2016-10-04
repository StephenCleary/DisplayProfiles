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
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { ContractResolver = DisplayConfigModeInfoContractResolver.Instance };

        public static DisplaySettings LoadDisplaySettings(string filename)
        {
            return JsonConvert.DeserializeObject<DisplaySettings>(File.ReadAllText(filename), JsonSerializerSettings)
                .UpdateAdapterIds();
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

                if (property.DeclaringType == typeof(NativeMethods.DisplayConfigModeInfo))
                {
                    if (property.PropertyName == "targetMode")
                    {
                        property.ShouldSerialize = obj =>
                        {
                            var instance = (NativeMethods.DisplayConfigModeInfo)obj;
                            return instance.infoType == NativeMethods.DisplayConfigModeInfoType.Target;
                        };
                    }
                    else if (property.PropertyName == "sourceMode")
                    {
                        property.ShouldSerialize = obj =>
                        {
                            var instance = (NativeMethods.DisplayConfigModeInfo)obj;
                            return instance.infoType == NativeMethods.DisplayConfigModeInfoType.Source;
                        };
                    }
                }

                return property;
            }
        }

    }
}
