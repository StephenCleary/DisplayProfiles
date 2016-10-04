using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DisplayProfiles.Interop
{
    public partial class NativeMethods
    {
        private static readonly IntPtr INVALID_HANDLE_VALUE = (IntPtr) (-1);
        private const uint DEVPROP_TYPE_STRING = 18;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct SpDeviceInterfaceData
        {
            public uint cbSize;
            public Guid interfaceClassGuid;
            public uint flags;
            public UIntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct SpDevInfoData
        {
            public uint cbSize;
            public Guid classGuid;
            public uint devInst;
            public UIntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct DevPropKey
        {
            public Guid fmtid;
            public uint pid;

            public static DevPropKey Device_FriendlyName => new DevPropKey
            {
                fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
                pid = 14,
            };
        }

        [DllImport("Setupapi.dll", EntryPoint = "SetupDiCreateDeviceInfoList", SetLastError = true)]
        private static extern IntPtr DoSetupDiCreateDeviceInfoList(IntPtr classGuid, IntPtr hwndParent);

        [DllImport("Setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        private static IntPtr SetupDiCreateDeviceInfoList()
        {
            var result = DoSetupDiCreateDeviceInfoList(IntPtr.Zero, IntPtr.Zero);
            if (result == INVALID_HANDLE_VALUE)
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            return result;
        }

        [DllImport("Setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiOpenDeviceInterface(IntPtr deviceInfoSet, string devicePath, uint openFlags, ref SpDeviceInterfaceData deviceInterfaceData);

        [DllImport("Setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDeleteDeviceInterfaceData(IntPtr deviceInfoSet, [In] ref SpDeviceInterfaceData deviceInterfaceData);

        private static void SetupDiOpenDeviceInterface(IntPtr deviceInfoSet, string devicePath, out SpDeviceInterfaceData deviceInterfaceData)
        {
            deviceInterfaceData = new SpDeviceInterfaceData { cbSize = (uint) Marshal.SizeOf(typeof(SpDeviceInterfaceData)) };
            if (!SetupDiOpenDeviceInterface(deviceInfoSet, devicePath, 0, ref deviceInterfaceData))
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        [DllImport("Setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex, ref SpDevInfoData deviceInfoData);

        private static void SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, out SpDevInfoData deviceInfoData)
        {
            // This wrapper only enumerates the first device in the device info set.
            deviceInfoData = new SpDevInfoData { cbSize = (uint) Marshal.SizeOf(typeof(SpDevInfoData)) };
            if (!SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData))
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        [DllImport("Setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDeviceProperty(IntPtr deviceInfoSet, [In] ref SpDevInfoData deviceInfoData, [In] ref DevPropKey propertyKey, out uint propertyType,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] [Out] byte[] buffer, uint bufferSize, out uint requiredSize, uint flags);

        private static string SetupDiGetDeviceProperty(IntPtr deviceInfoSet, ref SpDevInfoData deviceInfoData, DevPropKey propertyKey)
        {
            var localPropertyKey = propertyKey;
            uint propertyType;
            var buffer = new byte[0];
            uint requiredSize;

            // First call retrieves the necessary buffer size
            SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref localPropertyKey, out propertyType, buffer, (uint)buffer.Length, out requiredSize, 0);
            var err = Marshal.GetLastWin32Error();
            if (err != ERROR_INSUFICCIENT_BUFFER)
                throw Marshal.GetExceptionForHR(Win32ErrorToHResult(err));

            buffer = new byte[requiredSize];
            if (!SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref localPropertyKey, out propertyType, buffer, (uint)buffer.Length, out requiredSize, 0))
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

            // We only understand strings.
            if (propertyType != DEVPROP_TYPE_STRING)
                throw new NotSupportedException("Unknown property type " + propertyType);
            return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
        }

        /// <summary>
        /// Translates a device path into a device friendly name.
        /// </summary>
        /// <param name="devicePath">The device path</param>
        public static string GetDeviceFriendlyName(string devicePath)
        {
            var list = SetupDiCreateDeviceInfoList();
            try
            {
                SpDeviceInterfaceData interfaceData;
                SetupDiOpenDeviceInterface(list, devicePath, out interfaceData);
                try
                {
                    SpDevInfoData deviceInfoData;
                    SetupDiEnumDeviceInfo(list, out deviceInfoData);
                    return SetupDiGetDeviceProperty(list, ref deviceInfoData, DevPropKey.Device_FriendlyName);
                }
                finally
                {
                    SetupDiDeleteDeviceInterfaceData(list, ref interfaceData);
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(list);
            }
        }
    }
}
