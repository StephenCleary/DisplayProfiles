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
    }
}
