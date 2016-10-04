using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DisplayProfilesGui.Hotkeys
{
    public sealed class WinFormsHotkeyMessageFilter : IMessageFilter
    {
        private readonly Func<IntPtr, int, IntPtr, IntPtr, bool> _filter;

        public WinFormsHotkeyMessageFilter(Hotkey hotkey, Action callback)
        {
            _filter = hotkey.CreateMessageFilter(callback);
        }

        public bool PreFilterMessage(ref Message m)
        {
            return _filter(m.HWnd, m.Msg, m.WParam, m.LParam);
        }
    }
}
