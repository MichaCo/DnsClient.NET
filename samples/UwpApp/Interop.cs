using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UwpApp
{
    public class Interop
    {
        internal static partial class IpHlpApi
        {
            [Flags]
            internal enum AdapterFlags
            {
                DnsEnabled = 0x01,
                RegisterAdapterSuffix = 0x02,
                DhcpEnabled = 0x04,
                ReceiveOnly = 0x08,
                NoMulticast = 0x10,
                Ipv6OtherStatefulConfig = 0x20,
                NetBiosOverTcp = 0x40,
                IPv4Enabled = 0x80,
                IPv6Enabled = 0x100,
                IPv6ManagedAddressConfigurationSupported = 0x200,
            }

            [Flags]
            internal enum AdapterAddressFlags
            {
                DnsEligible = 0x1,
                Transient = 0x2
            }

            internal enum OldOperationalStatus
            {
                NonOperational = 0,
                Unreachable = 1,
                Disconnected = 2,
                Connecting = 3,
                Connected = 4,
                Operational = 5
            }

            [Flags]
            internal enum GetAdaptersAddressesFlags
            {
                SkipUnicast = 0x0001,
                SkipAnycast = 0x0002,
                SkipMulticast = 0x0004,
                SkipDnsServer = 0x0008,
                IncludePrefix = 0x0010,
                SkipFriendlyName = 0x0020,
                IncludeWins = 0x0040,
                IncludeGateways = 0x0080,
                IncludeAllInterfaces = 0x0100,
                IncludeAllCompartments = 0x0200,
                IncludeTunnelBindingOrder = 0x0400,
            }

            public const int MAX_HOSTNAME_LEN = 128;
            public const int MAX_DOMAIN_NAME_LEN = 128;
            public const int MAX_SCOPE_ID_LEN = 256;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct FIXED_INFO
            {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_HOSTNAME_LEN + 4)]
                public string hostName;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_DOMAIN_NAME_LEN + 4)]
                public string domainName;

                public IntPtr currentDnsServer; // IpAddressList*
                public IP_ADDR_STRING DnsServerList;
                public uint nodeType;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_SCOPE_ID_LEN + 4)]
                public string scopeId;

                public bool enableRouting;
                public bool enableProxy;
                public bool enableDns;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct IP_ADDR_STRING
            {
                public IntPtr Next; // struct _IpAddressList*

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
                public string IpAddress;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
                public string IpMask;

                public uint Context;
            }

            // IP_ADAPTER_ANYCAST_ADDRESS
            // IP_ADAPTER_MULTICAST_ADDRESS
            // IP_ADAPTER_DNS_SERVER_ADDRESS
            // IP_ADAPTER_WINS_SERVER_ADDRESS
            // IP_ADAPTER_GATEWAY_ADDRESS
            [StructLayout(LayoutKind.Sequential)]
            internal struct IpAdapterAddress
            {
                internal uint length;
                internal AdapterAddressFlags flags;
                internal IntPtr next;
                internal IpSocketAddress address;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct IpAdapterUnicastAddress
            {
                internal uint length;
                internal AdapterAddressFlags flags;
                internal IntPtr next;
                internal IpSocketAddress address;
                internal PrefixOrigin prefixOrigin;
                internal SuffixOrigin suffixOrigin;
                internal DuplicateAddressDetectionState dadState;
                internal uint validLifetime;
                internal uint preferredLifetime;
                internal uint leaseLifetime;
                internal byte prefixLength;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct IpAdapterAddresses
            {
                internal const int MAX_ADAPTER_ADDRESS_LENGTH = 8;

                internal uint length;
                internal uint index;
                internal IntPtr next;

                // Needs to be ANSI.
                [MarshalAs(UnmanagedType.LPStr)]
                internal string AdapterName;

                internal IntPtr firstUnicastAddress;
                internal IntPtr firstAnycastAddress;
                internal IntPtr firstMulticastAddress;
                internal IntPtr firstDnsServerAddress;

                internal string dnsSuffix;
                internal string description;
                internal string friendlyName;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ADAPTER_ADDRESS_LENGTH)]
                internal byte[] address;

                internal uint addressLength;
                internal AdapterFlags flags;
                internal uint mtu;
                internal NetworkInterfaceType type;
                internal OperationalStatus operStatus;
                internal uint ipv6Index;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                internal uint[] zoneIndices;

                internal IntPtr firstPrefix;

                internal UInt64 transmitLinkSpeed;
                internal UInt64 receiveLinkSpeed;
                internal IntPtr firstWinsServerAddress;
                internal IntPtr firstGatewayAddress;
                internal UInt32 ipv4Metric;
                internal UInt32 ipv6Metric;
                internal UInt64 luid;
                internal IpSocketAddress dhcpv4Server;
                internal UInt32 compartmentId;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                internal byte[] networkGuid;

                internal InterfaceConnectionType connectionType;
                internal InterfaceTunnelType tunnelType;
                internal IpSocketAddress dhcpv6Server; // Never available in Windows.

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 130)]
                internal byte[] dhcpv6ClientDuid;

                internal UInt32 dhcpv6ClientDuidLength;
                internal UInt32 dhcpV6Iaid;

                /* Windows 2008 +
                      PIP_ADAPTER_DNS_SUFFIX             FirstDnsSuffix;
                 * */
            }

            internal enum InterfaceConnectionType : int
            {
                Dedicated = 1,
                Passive = 2,
                Demand = 3,
                Maximum = 4,
            }

            internal enum InterfaceTunnelType : int
            {
                None = 0,
                Other = 1,
                Direct = 2,
                SixToFour = 11,
                Isatap = 13,
                Teredo = 14,
                IpHttps = 15,
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct IpSocketAddress
            {
                internal IntPtr address;
                internal int addressLength;

                public const int IPv6AddressSize = 28;
                public const int IPv4AddressSize = 16;

                internal IPAddress MarshalIPAddress()
                {
                    // Determine the address family used to create the IPAddress.
                    AddressFamily family = (addressLength > IPv6AddressSize)
                        ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

                    //TODO:
                    //Internals.SocketAddress sockAddress = new Internals.SocketAddress(family, addressLength);
                    //Marshal.Copy(address, sockAddress.Buffer, 0, addressLength);

                    //return sockAddress.GetIPAddress();
                    return IPAddress.Any;
                }
            }

            public const uint ERROR_SUCCESS = 0;
            public const uint ERROR_INVALID_FUNCTION = 1;
            public const uint ERROR_NO_SUCH_DEVICE = 2;
            public const uint ERROR_INVALID_DATA = 13;
            public const uint ERROR_INVALID_PARAMETER = 87;
            public const uint ERROR_BUFFER_OVERFLOW = 111;
            public const uint ERROR_INSUFFICIENT_BUFFER = 122;
            public const uint ERROR_NO_DATA = 232;
            public const uint ERROR_IO_PENDING = 997;
            public const uint ERROR_NOT_FOUND = 1168;

            [DllImport("iphlpapi.dll")]
            internal extern static uint GetAdaptersAddresses(
                AddressFamily family,
                uint flags,
                IntPtr pReserved,
                IntPtr adapterAddresses,
                ref uint outBufLen);

            [DllImport("iphlpapi.dll", ExactSpelling = true)]
            internal extern static uint GetNetworkParams(IntPtr pFixedInfo, ref uint pOutBufLen);

            public class FixedNetworkInformation
            {
                public List<IPAddress> DnsAddresses { get; private set; }

                public string DomainName { get; private set; }

                public string HostName { get; private set; }

                public static FixedNetworkInformation GetFixedNetworkInformation()
                {
                    var info = new FixedNetworkInformation();
                    uint size = 0;
                    DisposablePtr buffer = null;
                    Interop.IpHlpApi.FIXED_INFO fixedInfo = new Interop.IpHlpApi.FIXED_INFO();

                    // First we need to get the size of the buffer
                    uint result = Interop.IpHlpApi.GetNetworkParams(IntPtr.Zero, ref size);

                    while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
                    {
                        // Now we allocate the buffer and read the network parameters.
                        using (buffer = DisposablePtr.Alloc((int)size))
                        {
                            if (buffer.IsValid)
                            {
                                result = Interop.IpHlpApi.GetNetworkParams(buffer.Ptr, ref size);
                                if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                                {
                                    fixedInfo = Marshal.PtrToStructure<Interop.IpHlpApi.FIXED_INFO>(buffer.Ptr);
                                }
                            }
                            else
                            {
                                throw new OutOfMemoryException();
                            }
                        }
                    }

                    // If the result include there being no information, we'll still throw
                    if (result != Interop.IpHlpApi.ERROR_SUCCESS)
                    {
                        throw new Win32Exception((int)result);
                    }

                    var dnsAddresses = new List<IPAddress>();
                    IP_ADDR_STRING addr = fixedInfo.DnsServerList;
                    IPAddress ip;

                    if (IPAddress.TryParse(addr.IpAddress, out ip))
                    {
                        dnsAddresses.Add(ip);

                        while (addr.Next != IntPtr.Zero)
                        {
                            addr = Marshal.PtrToStructure<IP_ADDR_STRING>(addr.Next);
                            if (IPAddress.TryParse(addr.IpAddress, out ip))
                            {
                                dnsAddresses.Add(ip);
                            }
                        }
                    }

                    info.HostName = fixedInfo.hostName;

                    info.DomainName = fixedInfo.domainName;

                    info.DnsAddresses = dnsAddresses;

                    return info;
                }
            }

            public class SystemNetworkInterface : NetworkInterface
            {
                private readonly AdapterFlags _adapterFlags;
                private readonly uint _ipv6Index;
                private readonly long _speed;
                private readonly OperationalStatus _operStatus;
                private readonly NetworkInterfaceType _type;
                private readonly uint _addressLength;
                private readonly byte[] _physicalAddress;
                private readonly uint _index;
                private readonly string _description;
                private readonly string _name;
                private readonly string _id;
                private readonly FixedNetworkInformation _fixedInfo;

                public static new SystemNetworkInterface[] GetAllNetworkInterfaces()
                {
                    AddressFamily family = AddressFamily.Unspecified;
                    uint bufferSize = 0;
                    DisposablePtr buffer = null;

                    List<SystemNetworkInterface> interfaceList = new List<SystemNetworkInterface>();
                    var fixedInfo = FixedNetworkInformation.GetFixedNetworkInformation();

                    Interop.IpHlpApi.GetAdaptersAddressesFlags flags =
                        Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeGateways
                        | Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeWins;

                    // Figure out the right buffer size for the adapter information.
                    uint result = Interop.IpHlpApi.GetAdaptersAddresses(
                        family, (uint)flags, IntPtr.Zero, IntPtr.Zero, ref bufferSize);

                    while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
                    {
                        // Allocate the buffer and get the adapter info.
                        using (buffer = DisposablePtr.Alloc((int)bufferSize))
                        {
                            result = Interop.IpHlpApi.GetAdaptersAddresses(
                                family, (uint)flags, IntPtr.Zero, buffer.Ptr, ref bufferSize);

                            // If succeeded, we're going to add each new interface.
                            if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                            {
                                // Linked list of interfaces.
                                IntPtr ptr = buffer.Ptr;
                                while (ptr != IntPtr.Zero)
                                {
                                    // Traverse the list, marshal in the native structures, and create new NetworkInterfaces.
                                    Interop.IpHlpApi.IpAdapterAddresses adapterAddresses = Marshal.PtrToStructure<Interop.IpHlpApi.IpAdapterAddresses>(ptr);
                                    interfaceList.Add(new SystemNetworkInterface(fixedInfo, adapterAddresses));
                                    

                                    ptr = adapterAddresses.next;
                                }
                            }
                        }
                    }

                    return interfaceList.ToArray();
                }

                internal SystemNetworkInterface(FixedNetworkInformation fixedInfo, IpAdapterAddresses ipAdapterAddresses)
                {
                    _id = ipAdapterAddresses.AdapterName;
                    _name = ipAdapterAddresses.friendlyName;
                    _description = ipAdapterAddresses.description;
                    _index = ipAdapterAddresses.index;

                    _physicalAddress = ipAdapterAddresses.address;
                    _addressLength = ipAdapterAddresses.addressLength;

                    _type = ipAdapterAddresses.type;
                    _operStatus = ipAdapterAddresses.operStatus;
                    _speed = (long)ipAdapterAddresses.receiveLinkSpeed;

                    // API specific info.
                    _ipv6Index = ipAdapterAddresses.ipv6Index;

                    _fixedInfo = fixedInfo;

                    _adapterFlags = ipAdapterAddresses.flags;
                }

                public override string Id { get { return _id; } }

                public override string Name { get { return _name; } }

                public override string Description { get { return _description; } }

                public override PhysicalAddress GetPhysicalAddress()
                {
                    byte[] newAddr = new byte[_addressLength];

                    // Buffer.BlockCopy only supports int while addressLength is uint (see IpAdapterAddresses).
                    // Will throw OverflowException if addressLength > Int32.MaxValue.
                    Buffer.BlockCopy(_physicalAddress, 0, newAddr, 0, checked((int)_addressLength));
                    return new PhysicalAddress(newAddr);
                }

                public override NetworkInterfaceType NetworkInterfaceType { get { return _type; } }

                public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
                {
                    if (networkInterfaceComponent == NetworkInterfaceComponent.IPv6
                        && ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.IPv6Enabled) != 0))
                    {
                        return true;
                    }

                    if (networkInterfaceComponent == NetworkInterfaceComponent.IPv4
                        && ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.IPv4Enabled) != 0))
                    {
                        return true;
                    }

                    return false;
                }

                public override IPInterfaceProperties GetIPProperties()
                {
                    throw new NotImplementedException();
                }

                public override IPInterfaceStatistics GetIPStatistics()
                {
                    throw new NotImplementedException();
                }

                public override OperationalStatus OperationalStatus
                {
                    get
                    {
                        return _operStatus;
                    }
                }

                public override long Speed
                {
                    get
                    {
                        return _speed;
                    }
                }

                public override bool IsReceiveOnly
                {
                    get
                    {
                        return ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.ReceiveOnly) > 0);
                    }
                }

                /// <summary>The interface doesn't allow multicast.</summary>
                public override bool SupportsMulticast
                {
                    get
                    {
                        return ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.NoMulticast) == 0);
                    }
                }
            }
        }

        public class DisposablePtr : IDisposable
        {
            public IntPtr Ptr => _ptr;

            public bool IsValid { get; private set; } = true;

            private IntPtr _ptr;

            private DisposablePtr()
            {
            }

            public static DisposablePtr Alloc(int size)
            {
                var ptr = new DisposablePtr();
                try
                {
                    ptr._ptr = Marshal.AllocHGlobal(size);
                }
                catch (OutOfMemoryException)
                {
                    ptr.IsValid = false;
                }

                return ptr;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(_ptr);
            }
        }
    }
}
