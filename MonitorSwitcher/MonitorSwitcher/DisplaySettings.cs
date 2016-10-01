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

        public static DisplaySettings GetCurrent(bool activeOnly)
        {
            var queryFlags = activeOnly ?
                CCDWrapper.QueryDisplayFlags.OnlyActivePaths :
                CCDWrapper.QueryDisplayFlags.AllPaths;

            var arrays = CCDWrapper.GetDisplayConfig(queryFlags);
            return new DisplaySettings(arrays.Item1, arrays.Item2);
        }
    }
}
