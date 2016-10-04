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
            AdapterNames = new Dictionary<long, string>();
        }

        public DisplaySettings(IEnumerable<NativeMethods.DisplayConfigPathInfo> pathInfo, IEnumerable<NativeMethods.DisplayConfigModeInfo> modeInfo, Dictionary<long, string> adapterNames)
        {
            PathInfo = pathInfo.ToList();
            ModeInfo = modeInfo.ToList();
            AdapterNames = adapterNames;
        }

        public List<NativeMethods.DisplayConfigPathInfo> PathInfo { get; }
        public List<NativeMethods.DisplayConfigModeInfo> ModeInfo { get; }
        public Dictionary<long, string> AdapterNames { get; }

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
            var names = new Dictionary<long, string>();
            foreach (var adapterId in arrays.Item1.Select(x => x.sourceInfo.adapterId).Concat(arrays.Item1.Select(x => x.targetInfo.adapterId)).Concat(arrays.Item2.Select(x => x.adapterId)))
                if (!names.ContainsKey(adapterId) && adapterId != 0) // Sometimes we see invalid adapterId's of 0 when switching.
                    names.Add(adapterId, NativeMethods.GetAdapterName(adapterId));
            return new DisplaySettings(arrays.Item1, arrays.Item2, names);
        }

        private long UpdateAdapterId(long adapterId, DisplaySettings current)
        {
            var name = AdapterNames[adapterId];
            if (!current.AdapterNames.ContainsValue(name))
            {
                MissingAdapters.Add(name);
                return adapterId;
            }
            return current.AdapterNames.Where(x => x.Value == name).Select(x => x.Key).First();
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
    }
}
