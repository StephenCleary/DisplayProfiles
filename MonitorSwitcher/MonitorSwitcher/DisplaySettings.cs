using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MonitorSwitcherGUI
{
    public sealed class DisplaySettings
    {
        public DisplaySettings()
        {
            PathInfo = new List<CCDWrapper.DisplayConfigPathInfo>();
            ModeInfo = new List<CCDWrapper.DisplayConfigModeInfo>();
        }

        public DisplaySettings(IEnumerable<CCDWrapper.DisplayConfigPathInfo> pathInfo, IEnumerable<CCDWrapper.DisplayConfigModeInfo> modeInfo)
        {
            PathInfo = pathInfo.ToList();
            ModeInfo = modeInfo.ToList();
        }

        public List<CCDWrapper.DisplayConfigPathInfo> PathInfo { get; }
        public List<CCDWrapper.DisplayConfigModeInfo> ModeInfo { get; }

        public void SetCurrent()
        {
            var flags = CCDWrapper.SdcFlags.Apply | CCDWrapper.SdcFlags.UseSuppliedDisplayConfig | CCDWrapper.SdcFlags.SaveToDatabase | CCDWrapper.SdcFlags.AllowChanges;
            CCDWrapper.SetDisplayConfig(PathInfo.ToArray(), ModeInfo.ToArray(), flags);
        }

        public static DisplaySettings GetCurrent(bool activeOnly)
        {
            var flags = activeOnly ?
                CCDWrapper.QueryDisplayFlags.OnlyActivePaths :
                CCDWrapper.QueryDisplayFlags.AllPaths;

            var arrays = CCDWrapper.GetDisplayConfig(flags);
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
                var j = PathInfo.FindIndex(x => x.targetInfo.id == targetMode.id && targetMode.infoType == CCDWrapper.DisplayConfigModeInfoType.Target);
                if (j == -1)
                    continue;
                var path = PathInfo[j];

                // We found target adapter id, now lets look for the source modeInfo and adapterID
                var k = ModeInfo.FindIndex(x => x.id == path.sourceInfo.id && x.adapterId == targetMode.adapterId && x.infoType == CCDWrapper.DisplayConfigModeInfoType.Source);
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
