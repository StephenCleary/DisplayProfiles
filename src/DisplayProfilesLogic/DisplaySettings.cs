using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DisplayProfiles.Interop;

namespace DisplayProfiles
{
    public sealed class DisplaySettings
    {
        public DisplaySettings()
        {
            PathInfo = new List<Ccd.DisplayConfigPathInfo>();
            ModeInfo = new List<Ccd.DisplayConfigModeInfo>();
        }

        public DisplaySettings(IEnumerable<Ccd.DisplayConfigPathInfo> pathInfo, IEnumerable<Ccd.DisplayConfigModeInfo> modeInfo)
        {
            PathInfo = pathInfo.ToList();
            ModeInfo = modeInfo.ToList();
        }

        public List<Ccd.DisplayConfigPathInfo> PathInfo { get; }
        public List<Ccd.DisplayConfigModeInfo> ModeInfo { get; }

        public void SetCurrent()
        {
            var flags = Ccd.SdcFlags.Apply | Ccd.SdcFlags.UseSuppliedDisplayConfig | Ccd.SdcFlags.SaveToDatabase | Ccd.SdcFlags.AllowChanges;
            Ccd.SetDisplayConfig(PathInfo.ToArray(), ModeInfo.ToArray(), flags);
        }

        public static DisplaySettings GetCurrent(bool activeOnly)
        {
            var flags = activeOnly ?
                Ccd.QueryDisplayFlags.OnlyActivePaths :
                Ccd.QueryDisplayFlags.AllPaths;

            var arrays = Ccd.GetDisplayConfig(flags);
            return new DisplaySettings(arrays.Item1, arrays.Item2);
        }

        public DisplaySettings UpdateAdapterIds()
        {
            // For some reason the adapterID parameter changes upon system restart, all other parameters however, especially the ID remain constant.
            // We check the loaded settings against the current settings replacing the adapaterID with the other parameters

            var current = GetCurrent(activeOnly: false);
            for (var i = 0; i != PathInfo.Count; ++i)
            {
                var path = PathInfo[i];
                var j = current.PathInfo.FindIndex(x => x.sourceInfo.id == path.sourceInfo.id && x.targetInfo.id == path.targetInfo.id);
                if (j == -1)
                    continue;
                path.sourceInfo.adapterId = current.PathInfo[j].sourceInfo.adapterId;
                path.targetInfo.adapterId = current.PathInfo[j].targetInfo.adapterId;
                PathInfo[i] = path;
            }

            // Same again for modeInfo, however we get the required adapterId information from the pathInfoArray
            for (var i = 0; i != ModeInfo.Count; ++i)
            {
                var targetMode = ModeInfo[i];
                var j = PathInfo.FindIndex(x => x.targetInfo.id == targetMode.id && targetMode.infoType == Ccd.DisplayConfigModeInfoType.Target);
                if (j == -1)
                    continue;
                var path = PathInfo[j];

                // We found target adapter id, now lets look for the source modeInfo and adapterID
                var k = ModeInfo.FindIndex(x => x.id == path.sourceInfo.id && x.adapterId == targetMode.adapterId && x.infoType == Ccd.DisplayConfigModeInfoType.Source);
                if (k != -1)
                {
                    var sourceMode = ModeInfo[k];
                    sourceMode.adapterId = path.sourceInfo.adapterId;
                    ModeInfo[k] = sourceMode;
                }

                targetMode.adapterId = path.targetInfo.adapterId;
                ModeInfo[i] = targetMode;
            }

            return this;
        }
    }
}
