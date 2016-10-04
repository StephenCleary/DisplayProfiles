using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DisplayProfilesGui.DeviceChangeNotification
{
    public static class NativeMethods
    {
        public const int WM_DEVICECHANGE = 0x0219;
        public static readonly IntPtr DBT_DEVICEQUERYREMOVE = (IntPtr)0x8001;
        public static readonly IntPtr DBT_DEVICEREMOVEPENDING = (IntPtr)0x8003;
        public static readonly IntPtr DBT_QUERYCHANGECONFIG = (IntPtr)0x0017;

        private const uint DBT_DEVTYP_DEVICEINTERFACE = 0x5;
        private const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x0;
        private const uint DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x4;

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
        private struct DevBroadcastDeviceInterface
        {
            public uint size;
            public uint deviceType;
            public uint reserved;
            public Guid classGuid;
            public char name;
        }

        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, ref DevBroadcastDeviceInterface notificationFilter, uint flags);

        public static void RegisterForDeviceNotification(IntPtr window)
        {
            var filter = new DevBroadcastDeviceInterface
            {
                size = (uint) Marshal.SizeOf(typeof(DevBroadcastDeviceInterface)),
                deviceType = DBT_DEVTYP_DEVICEINTERFACE,
            };
            var result = RegisterDeviceNotification(window, ref filter, DEVICE_NOTIFY_WINDOW_HANDLE | DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);
            if (result == IntPtr.Zero)
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }
}
