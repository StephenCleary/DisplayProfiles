using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace DisplayProfiles.Interop
{
    public class NativeMethods
    {
        private const int NO_ERROR = 0;
        private const int ERROR_INSUFICCIENT_BUFFER = 122;

        [Flags]
        public enum SdcFlags : uint
        {
            TopologyInternal = 0x00000001,
            TopologyClone = 0x00000002,
            TopologyExtend = 0x00000004,
            TopologyExternal = 0x00000008,
            TopologySupplied = 0x00000010,

            UseSuppliedDisplayConfig = 0x00000020,
            Validate = 0x00000040,
            Apply = 0x00000080,
            NoOptimization = 0x00000100,
            SaveToDatabase = 0x00000200,
            AllowChanges = 0x00000400,
            PathPersistIfRequired = 0x00000800,
            ForceModeEnumeration = 0x00001000,
            AllowPathOrderChanges = 0x00002000,

            UseDatabaseCurrent = TopologyInternal | TopologyClone | TopologyExtend | TopologyExternal
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DisplayConfigRational
        {
            public uint numerator;
            public uint denominator;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DisplayConfigPathInfo
        {
            public DisplayConfigPathSourceInfo sourceInfo;
            public DisplayConfigPathTargetInfo targetInfo;
            public uint flags;
        }

        [Flags]
        public enum DisplayConfigModeInfoType : uint
        {
            Source = 1,
            Target = 2,
            _ = 0xFFFFFFFF
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DisplayConfigModeInfo
        {
            [FieldOffset((0))]
            [XmlIgnore]
            public DisplayConfigModeInfoType infoType;

            [XmlElement("infoType")]
            [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
            public uint infoTypeValue { get { return (uint)infoType; } set { infoType = (DisplayConfigModeInfoType)value; } }

            [FieldOffset(4)]
            public uint id;

            [FieldOffset(8)]
            public long adapterId;

            [FieldOffset(16)]
            public DisplayConfigTargetMode targetMode;

            [FieldOffset(16)]
            public DisplayConfigSourceMode sourceMode;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DisplayConfig2DRegion
        {
            public uint cx;
            public uint cy;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DisplayConfigVideoSignalInfo
        {
            public ulong pixelRate;
            public DisplayConfigRational hSyncFreq;
            public DisplayConfigRational vSyncFreq;
            public DisplayConfig2DRegion activeSize;
            public DisplayConfig2DRegion totalSize;
            public uint videoStandard;
            public uint scanLineOrdering;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DisplayConfigTargetMode
        {
            public DisplayConfigVideoSignalInfo targetVideoSignalInfo;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PointL
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DisplayConfigSourceMode
        {
            public uint width;
            public uint height;
            public uint pixelFormat;
            public PointL position;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DisplayConfigPathSourceInfo
        {
            public long adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DisplayConfigPathTargetInfo
        {
            public long adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public DisplayConfigRational refreshRate;
            public uint scanLineOrdering;
            public uint targetAvailable;
            public uint statusFlags;
        }

        private enum DisplayConfigDeviceInfoType: uint
        {
            GetAdapterName = 4,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
        public struct DisplayConfigAdapterName
        {
            public DisplayConfigAdapterName(long adapterId)
                : this()
            {
                type = (uint)DisplayConfigDeviceInfoType.GetAdapterName;
                size = (uint)Marshal.SizeOf(typeof(DisplayConfigAdapterName));
                this.adapterId = adapterId;
            }

            public uint type;
            public uint size;
            public long adapterId;
            public uint id;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            public string adapterDevicePath;
        }

        [Flags]
        public enum QueryDisplayFlags : uint
        {
            AllPaths = 0x00000001,
            OnlyActivePaths = 0x00000002,
            DatabaseCurrent = 0x00000004
        }

        [DllImport("User32.dll")]
        private static extern int SetDisplayConfig(
            uint numPathArrayElements,
            [In] DisplayConfigPathInfo[] pathArray,
            uint numModeInfoArrayElements,
            [In] DisplayConfigModeInfo[] modeInfoArray,
            SdcFlags flags
        );

        public static void SetDisplayConfig(DisplayConfigPathInfo[] pathArray, DisplayConfigModeInfo[] modeInfoArray, SdcFlags flags)
        {
            var err = SetDisplayConfig((uint)pathArray.Length, pathArray, (uint)modeInfoArray.Length, modeInfoArray, flags);
            if (err != NO_ERROR)
                throw new Win32Exception(err);
        }

        [DllImport("User32.dll")]
        private static extern int GetDisplayConfigBufferSizes(QueryDisplayFlags flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        [DllImport("User32.dll")]
        private static extern int QueryDisplayConfig(
            QueryDisplayFlags flags,
            ref uint numPathArrayElements,
            [Out] DisplayConfigPathInfo[] pathInfoArray,
            ref uint modeInfoArrayElements,
            [Out] DisplayConfigModeInfo[] modeInfoArray,
            IntPtr z
        );

        public static Tuple<DisplayConfigPathInfo[], DisplayConfigModeInfo[]> GetDisplayConfig(QueryDisplayFlags flags)
        {
            while (true)
            {
                uint numPathArrayElements, numModeInfoArrayElements;
                var err = GetDisplayConfigBufferSizes(flags, out numPathArrayElements, out numModeInfoArrayElements);
                if (err != NO_ERROR)
                   throw Marshal.GetExceptionForHR(Win32ErrorToHResult(err));

                var pathArray = new DisplayConfigPathInfo[numPathArrayElements];
                var modeArray = new DisplayConfigModeInfo[numModeInfoArrayElements];
                err = QueryDisplayConfig(flags, ref numPathArrayElements, pathArray, ref numModeInfoArrayElements, modeArray, IntPtr.Zero);
                if (err == ERROR_INSUFICCIENT_BUFFER)
                    continue;
                if (err != NO_ERROR)
                    throw Marshal.GetExceptionForHR(Win32ErrorToHResult(err));
                return Tuple.Create(pathArray, modeArray);
            }
        }

        [DllImport("User32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref DisplayConfigAdapterName info);

        public static string GetAdapterName(long adapterId)
        {
            var info = new DisplayConfigAdapterName(adapterId);
            var err = DisplayConfigGetDeviceInfo(ref info);
            if (err != NO_ERROR)
                throw Marshal.GetExceptionForHR(Win32ErrorToHResult(err));
            return info.adapterDevicePath;
        }

        private static int Win32ErrorToHResult(int win32Error)
        {
            if (win32Error == 0)
                return 0;
            var err = unchecked((uint) win32Error);
            var result = (err & 0x0000FFFF) | 0x80070000;
            return unchecked((int) result);
        }
    }
}
