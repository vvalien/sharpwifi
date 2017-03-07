using System;
using System.Text;
using System.Runtime.InteropServices;
// Most code taken from pinvoke, stackoverflow and...
// https://github.com/DigiExam/simplewifi <-- thank you

namespace wifi_sharp
{
    class Program
    {
        public enum WLAN_INTERFACE_STATE
        {
            wlan_interface_state_not_ready = 0,
            wlan_interface_state_connected = 1,
            wlan_interface_state_ad_hoc_network_formed = 2,
            wlan_interface_state_disconnecting = 3,
            wlan_interface_state_disconnected = 4,
            wlan_interface_state_associating = 5,
            wlan_interface_state_discovering = 6,
            wlan_interface_state_authenticating = 7,
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_INTERFACE_INFO
        {
            /// GUID->_GUID
            public Guid InterfaceGuid;

            /// WCHAR[256]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;

            /// WLAN_INTERFACE_STATE->_WLAN_INTERFACE_STATE
            public WLAN_INTERFACE_STATE isState;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct WLAN_INTERFACE_INFO_LIST
        {
            public Int32 dwNumberOfItems;
            public Int32 dwIndex;
            public WLAN_INTERFACE_INFO[] InterfaceInfo;
            public WLAN_INTERFACE_INFO_LIST(IntPtr pList)
            {
                // The first 4 bytes are the number of WLAN_INTERFACE_INFO structures.
                dwNumberOfItems = Marshal.ReadInt32(pList, 0);
                // The next 4 bytes are the index of the current item in the unmanaged API.
                dwIndex = Marshal.ReadInt32(pList, 4);
                // Construct the array of WLAN_INTERFACE_INFO structures.
                InterfaceInfo = new WLAN_INTERFACE_INFO[dwNumberOfItems];
                for (int i = 0; i <= dwNumberOfItems - 1; i++)
                {
                    // The offset of the array of structures is 8 bytes past the beginning.
                    // Then, take the index and multiply it by the number of bytes in the structure.
                    // The length of the WLAN_INTERFACE_INFO structure is 532 bytes - this
                    // was determined by doing a Marshall.SizeOf(WLAN_INTERFACE_INFO) 
                    IntPtr pItemList = new IntPtr(pList.ToInt64() + (i * 532) + 8);
                    InterfaceInfo[i] = (WLAN_INTERFACE_INFO)Marshal.PtrToStructure(pItemList, typeof(WLAN_INTERFACE_INFO));
                }
            }
        }
        /// <summary>
        /// ////////
        /// </summary>
        public enum Dot11BssType
        {
            Infrastructure = 1,
            Independent = 2,
            Any = 3
        }
        public enum Dot11PhyType : uint
        {
            Unknown = 0,
            Any = Unknown,
            FHSS = 1,
            DSSS = 2,
            IrBaseband = 3,
            OFDM = 4,
            HRDSSS = 5,
            ERP = 6,
            IHV_Start = 0x80000000,
            IHV_End = 0xffffffff
        }
        public struct Dot11Ssid
        {
            public uint SSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] SSID;
        }
        public struct WlanRateSet
        {
            private uint rateSetLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 126)]
            private ushort[] rateSet;

            public ushort[] Rates
            {
                get
                {
                    ushort[] rates = new ushort[rateSetLength / sizeof(ushort)];
                    Array.Copy(rateSet, rates, rates.Length);
                    return rates;
                }
            }
            public double GetRateInMbps(int rate)
            {
                return (rateSet[rate] & 0x7FFF) * 0.5;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WlanInterfaceInfoListHeader
        {
            public uint numberOfItems;
            public uint index;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanInterfaceInfo
        {
            public Guid interfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string interfaceDescription;
            public WLAN_INTERFACE_STATE isState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WlanBssEntry
        {
            public Dot11Ssid dot11Ssid;
            public uint phyId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] dot11Bssid;
            public Dot11BssType dot11BssType;
            public Dot11PhyType dot11BssPhyType;
            public int rssi;
            public uint linkQuality;
            public bool inRegDomain;
            public ushort beaconPeriod;
            public ulong timestamp;
            public ulong hostTimestamp;
            public ushort capabilityInformation;
            public uint chCenterFrequency;
            public WlanRateSet wlanRateSet;
            public uint ieOffset;
            public uint ieSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WlanBssListHeader
        {
            internal uint totalSize;
            internal uint numberOfItems;
        }

        // the magic
        public static WlanBssEntry[] ConvertBssListPtr(IntPtr bssListPtr)
        {
            WlanBssListHeader bssListHeader = (WlanBssListHeader)Marshal.PtrToStructure(bssListPtr, typeof(WlanBssListHeader));
            long bssListIt = bssListPtr.ToInt64() + Marshal.SizeOf(typeof(WlanBssListHeader));
            WlanBssEntry[] bssEntries = new WlanBssEntry[bssListHeader.numberOfItems];
            for (int i = 0; i < bssListHeader.numberOfItems; ++i)
            {
                bssEntries[i] = (WlanBssEntry)Marshal.PtrToStructure(new IntPtr(bssListIt), typeof(WlanBssEntry));
                bssListIt += Marshal.SizeOf(typeof(WlanBssEntry));
            }
            return bssEntries;
        }


        [DllImport("wlanapi.dll")]
        public static extern int WlanEnumInterfaces(
            [In] IntPtr clientHandle,
            [In, Out] IntPtr pReserved,
            [Out] out IntPtr ppInterfaceList);

        [DllImport("Wlanapi.dll")]
        private static extern int WlanOpenHandle(
            uint dwClientVersion,
            IntPtr pReserved, //not in MSDN but required
            [Out] out uint pdwNegotiatedVersion,
            out IntPtr ClientHandle);

        [DllImport("wlanapi.dll")]
        public static extern int WlanGetNetworkBssList(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] IntPtr dot11SsidInt,
            [In] Dot11BssType dot11BssType,
            [In] bool securityEnabled,
            IntPtr reservedPtr,
            [Out] out IntPtr wlanBssList
        );

        public static void Awesome()
        {
            IntPtr ppAvailableNetworkList = new IntPtr(0);
            IntPtr ClientHandle = IntPtr.Zero;
            uint negotiatedVersion;
            WlanOpenHandle(1, IntPtr.Zero, out negotiatedVersion, out ClientHandle);

            IntPtr ifaceList = new IntPtr();
            WlanEnumInterfaces(ClientHandle, IntPtr.Zero, out ifaceList);

            WlanInterfaceInfoListHeader header = (WlanInterfaceInfoListHeader)Marshal.PtrToStructure(ifaceList, typeof(WlanInterfaceInfoListHeader));
            Int64 listIterator = ifaceList.ToInt64() + Marshal.SizeOf(header);

            // easier way, but only works on 1 adapter?
            //WlanInterfaceInfo info = (WlanInterfaceInfo)Marshal.PtrToStructure(new IntPtr(listIterator), typeof(WlanInterfaceInfo));
            WLAN_INTERFACE_INFO_LIST infoList = new WLAN_INTERFACE_INFO_LIST(ifaceList);

            Guid pInterfaceGuid = ((WLAN_INTERFACE_INFO)infoList.InterfaceInfo[0]).InterfaceGuid;

            // little debugging...
            Console.WriteLine("iface: {0} ~ {1}", pInterfaceGuid, infoList.InterfaceInfo[0].isState);

            IntPtr bssListPtr = IntPtr.Zero;
            WlanGetNetworkBssList(ClientHandle, pInterfaceGuid, IntPtr.Zero, Dot11BssType.Any, true, IntPtr.Zero, out bssListPtr);


            WlanBssEntry[] APs = ConvertBssListPtr(bssListPtr);
            for (int i = 0; i < APs.Length; i++)
            {
                String bsi = BitConverter.ToString(APs[i].dot11Bssid);
                Console.Write(bsi.Replace("-", ":"));

                String ssidName = Encoding.ASCII.GetString(APs[i].dot11Ssid.SSID, 0, (int)APs[i].dot11Ssid.SSIDLength);
                Console.WriteLine("  ~  {0}", ssidName);
                // we can use almost every part of the WlanBssEntry struct for transmission to client
                // for transmission back we are mostly limited to requesting ssid names...
                // we can also use eventing and or a wmi implant to help ;)
                // shellcode should also be possible...
            }
        }
    }

    class WifiCheck
    {
        static void Main(string[] args)
        {
            Program.Awesome();
        }
    }
}
