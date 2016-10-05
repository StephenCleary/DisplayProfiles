using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace DisplayProfilesGui
{
    public sealed class HotkeySetting
    {
        public string Id { get; set; }

        public Keys Hotkey { get; set; }
    }
}
