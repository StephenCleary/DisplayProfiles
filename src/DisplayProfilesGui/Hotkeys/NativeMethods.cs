using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DisplayProfilesGui.Hotkeys
{
    public static class NativeMethods
    {
        public const int ERROR_HOTKEY_ALREADY_REGISTERED = unchecked((int)0x80070581);

        private const int WM_HOTKEY = 0x0312;

        [Flags]
        public enum KeyModifiers : uint
        {
            None = 0x0,
            Alt = 0x1,
            Control = 0x2,
            Shift = 0x4,
            NoRepeat = 0x4000
        }

        [DllImport("user32.dll", EntryPoint = "RegisterHotKey", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DoRegisterHotKey(IntPtr hWnd, int id, KeyModifiers fsModifiers, uint vk);

        public static void RegisterHotKey(IntPtr hWnd, int id, KeyModifiers fsModifiers, uint vk)
        {
            if (!DoRegisterHotKey(hWnd, id, fsModifiers, vk))
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        [DllImport("user32.dll", EntryPoint = "UnregisterHotKey", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DoUnregisterHotKey(IntPtr hWnd, int id);

        public static void UnregisterHotKey(IntPtr hWnd, int id)
        {
            if (!DoUnregisterHotKey(hWnd, id))
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        public static Func<IntPtr, int, IntPtr, IntPtr, bool> CreateMessageFilter(IntPtr hWnd, int id, Action callback)
        {
            return (hwnd, msg, wParam, lParam) =>
            {
                if (hwnd != hWnd || msg != WM_HOTKEY || wParam != (IntPtr) id)
                    return false;
                callback();
                return true;
            };
        }
    }
}
