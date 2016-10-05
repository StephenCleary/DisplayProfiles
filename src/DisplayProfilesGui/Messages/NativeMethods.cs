using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DisplayProfilesGui.Messages
{
    public static class NativeMethods
    {
        private static readonly IntPtr HWND_BROADCAST = (IntPtr) 0xFFFF;
        private const uint WM_SYSCOMMAND = 0x0112;
        private static readonly IntPtr SC_MONITORPOWER = (IntPtr) 0xF170;
        private static readonly IntPtr SC_MONITORPOWER_lParam_PowerOff = (IntPtr) 2;

        [DllImport("User32.dll", EntryPoint = "PostMessage", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DoPostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static void PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (!DoPostMessage(hWnd, msg, wParam, lParam))
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        public static void TurnMonitorsOff()
        {
            PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, SC_MONITORPOWER_lParam_PowerOff);
        }
    }
}
