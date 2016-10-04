using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DisplayProfiles.Interop;
using Newtonsoft.Json;

// Relationship of Mode Information to Path Information:
//  https://msdn.microsoft.com/en-us/library/windows/hardware/ff569241(v=vs.85).aspx (http://www.webcitation.org/6kzhHphCU)
// http://stackoverflow.com/questions/22399622/windows-multi-monitor-how-can-i-determine-if-a-target-is-physically-connected-t (http://www.webcitation.org/6kzhS0qnv)
// Video Present Network Terminology:
//  https://msdn.microsoft.com/en-us/library/windows/hardware/ff570543(v=vs.85).aspx (http://www.webcitation.org/6kzhdNyRf)

namespace DisplayProfiles
{
    public sealed class DisplaySettings
    {
        public DisplaySettings()
        {
            PathInfo = new List<NativeMethods.DisplayConfigPathInfo>();
            ModeInfo = new List<NativeMethods.DisplayConfigModeInfo>();
            Adapters = new Dictionary<long, AdapterData>();
        }

        public DisplaySettings(IEnumerable<NativeMethods.DisplayConfigPathInfo> pathInfo, IEnumerable<NativeMethods.DisplayConfigModeInfo> modeInfo, Dictionary<long, AdapterData> adapterData)
        {
            PathInfo = pathInfo.ToList();
            ModeInfo = modeInfo.ToList();
            Adapters = adapterData;
        }

        public List<NativeMethods.DisplayConfigPathInfo> PathInfo { get; }
        public List<NativeMethods.DisplayConfigModeInfo> ModeInfo { get; }
        public Dictionary<long, AdapterData> Adapters { get; }

        [JsonIgnore]
        public HashSet<string> MissingAdapters { get; } = new HashSet<string> ();

        public void SetCurrent()
        {
            NativeMethods.SetDisplayConfig(PathInfo.ToArray(), ModeInfo.ToArray(), NativeMethods.SdcFlags.Apply | NativeMethods.SdcFlags.UseSuppliedDisplayConfig | NativeMethods.SdcFlags.AllowChanges | NativeMethods.SdcFlags.SaveToDatabase);
        }

        public Exception Validate()
        {
            try
            {
                NativeMethods.SetDisplayConfig(PathInfo.ToArray(), ModeInfo.ToArray(), NativeMethods.SdcFlags.Validate | NativeMethods.SdcFlags.UseSuppliedDisplayConfig | NativeMethods.SdcFlags.AllowChanges);
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }

        public static DisplaySettings GetCurrent(bool activeOnly)
        {
            var flags = activeOnly ?
                NativeMethods.QueryDisplayFlags.OnlyActivePaths :
                NativeMethods.QueryDisplayFlags.AllPaths;

            var arrays = NativeMethods.GetDisplayConfig(flags);
            var paths = arrays.Item1;
            var modes = arrays.Item2;
            var names = new Dictionary<long, AdapterData>();
            foreach (var adapterId in paths.Select(x => x.sourceInfo.adapterId).Concat(paths.Select(x => x.targetInfo.adapterId)).Concat(modes.Select(x => x.adapterId)).Distinct())
            {
                // Sometimes we see invalid adapterId's of 0 when switching.
                if (adapterId == 0)
                    continue;
                var data = new AdapterData(NativeMethods.GetAdapterName(adapterId));
                foreach (var sourceId in paths.Where(x => x.sourceInfo.adapterId == adapterId).Select(x => x.sourceInfo.id)
                    .Concat(modes.Where(x => x.adapterId == adapterId && x.infoType == NativeMethods.DisplayConfigModeInfoType.Source).Select(x => x.id)).Distinct())
                {
                    data.Sources.Add(sourceId, new SourceData(NativeMethods.GetSourceName(adapterId, sourceId)));
                }
                foreach (var targetId in paths.Where(x => x.targetInfo.adapterId == adapterId).Select(x => x.targetInfo.id)
                    .Concat(modes.Where(x => x.adapterId == adapterId && x.infoType == NativeMethods.DisplayConfigModeInfoType.Target).Select(x => x.id)).Distinct())
                {
                    var targetNames = NativeMethods.GetTargetNames(adapterId, targetId);
                    data.Targets.Add(targetId, new TargetData(targetNames.Item1, targetNames.Item2));
                }
                names.Add(adapterId, data);
            }
            return new DisplaySettings(arrays.Item1, arrays.Item2, names);
        }

        private long UpdateAdapterId(long adapterId, DisplaySettings current)
        {
            var name = Adapters[adapterId].Name;
            if (current.Adapters.All(x => x.Value.Name != name))
            {
                MissingAdapters.Add(name);
                return adapterId;
            }
            return current.Adapters.Where(x => x.Value.Name == name).Select(x => x.Key).First();
        }

        public DisplaySettings UpdateAdapterIds()
        {
            // The adapterId's sometimes change after a system restart.
            // Make a best-effort to update the adapterId's with what's currently in the system.
            MissingAdapters.Clear();
            var current = GetCurrent(activeOnly: false);

            for (var i = 0; i != PathInfo.Count; ++i)
            {
                var path = PathInfo[i];
                path.sourceInfo.adapterId = UpdateAdapterId(path.sourceInfo.adapterId, current);
                path.targetInfo.adapterId = UpdateAdapterId(path.targetInfo.adapterId, current);
                PathInfo[i] = path;
            }

            for (var i = 0; i != ModeInfo.Count; ++i)
            {
                var mode = ModeInfo[i];
                mode.adapterId = UpdateAdapterId(mode.adapterId, current);
                ModeInfo[i] = mode;
            }

            return this;
        }

        public sealed class AdapterData
        {
            public AdapterData(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public Dictionary<uint, SourceData> Sources { get; } = new Dictionary<uint, SourceData>();
            public Dictionary<uint, TargetData> Targets { get; } = new Dictionary<uint, TargetData>();
        }

        public sealed class SourceData
        {
            public SourceData(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        public sealed class TargetData
        {
            public TargetData(string friendlyName, string name)
            {
                FriendlyName = friendlyName;
                Name = name;
            }

            public string FriendlyName { get; }
            public string Name { get; }
        }
    }
}
