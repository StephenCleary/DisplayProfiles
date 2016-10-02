using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MonitorSwitcherGUI
{
    public sealed class DisplayConfigModeInfoContractResolver : DefaultContractResolver
    {
        public static readonly DisplayConfigModeInfoContractResolver Instance = new DisplayConfigModeInfoContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType == typeof(CCDWrapper.DisplayConfigModeInfo))
            {
                if (property.PropertyName == "targetMode")
                {
                    property.ShouldSerialize = obj =>
                    {
                        var instance = (CCDWrapper.DisplayConfigModeInfo) obj;
                        return instance.infoType == CCDWrapper.DisplayConfigModeInfoType.Target;
                    };
                }
                else if (property.PropertyName == "sourceMode")
                {
                    property.ShouldSerialize = obj =>
                    {
                        var instance = (CCDWrapper.DisplayConfigModeInfo)obj;
                        return instance.infoType == CCDWrapper.DisplayConfigModeInfoType.Source;
                    };
                }
            }

            return property;
        }
    }
}
