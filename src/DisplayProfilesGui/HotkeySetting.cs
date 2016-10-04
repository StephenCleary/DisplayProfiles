using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DisplayProfilesGui
{
    public sealed class HotkeySetting
    {
        public string ProfileName { get; set; }
        public Keys Hotkey { get; set; }
    }
}
