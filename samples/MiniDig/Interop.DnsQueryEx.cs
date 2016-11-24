using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DigApp
{
    public partial class Interop
    {
        public class DNSQueryer
        {
            private const int DnsRecordsNoInfo = 9501;
            private const int DnsRequestPending = 9506;
            private const int DnsAddrMaxSockaddrLength = 32;
            private const int DNSQueryCancelSize = 32;
            private const short DNSPort = 53;
            private const short AFInet = 2;
            private const short AFInet16 = 23;
            private const int IpAddressV6LengthBytes = 16;
            private const int IpAddressV4LengthBytes = 4;
            private const uint DnsQueryRequestVersion1 = 1;

            private delegate void QueryCompletionRoutineFunctionPointer(IntPtr queryContext, IntPtr queryResults);

            [Flags]
            private enum DnsQueryOptions
            {
                DNS_QUERY_STANDARD = 0x0,
                DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 0x1,
                DNS_QUERY_USE_TCP_ONLY = 0x2,
                DNS_QUERY_NO_RECURSION = 0x4,
                DNS_QUERY_BYPASS_CACHE = 0x8,
                DNS_QUERY_NO_WIRE_QUERY = 0x10,
                DNS_QUERY_NO_LOCAL_NAME = 0x20,
                DNS_QUERY_NO_HOSTS_FILE = 0x40,
                DNS_QUERY_NO_NETBT = 0x80,
                DNS_QUERY_WIRE_ONLY = 0x100,
                DNS_QUERY_RETURN_MESSAGE = 0x200,
                DNS_QUERY_MULTICAST_ONLY = 0x400,
                DNS_QUERY_NO_MULTICAST = 0x800,
                DNS_QUERY_TREAT_AS_FQDN = 0x1000,
                DNS_QUERY_ADDRCONFIG = 0x2000,
                DNS_QUERY_DUAL_ADDR = 0x4000,
                DNS_QUERY_MULTICAST_WAIT = 0x20000,
                DNS_QUERY_MULTICAST_VERIFY = 0x40000,
                DNS_QUERY_DONT_RESET_TTL_VALUES = 0x100000,
                DNS_QUERY_DISABLE_IDN_ENCODING = 0x200000,
                DNS_QUERY_APPEND_MULTILABEL = 0x800000,
                DNS_QUERY_RESERVED = unchecked((int)0xF0000000)
            }

            public enum DnsRecordTypes
            {
                DNS_TYPE_A = 0x1,
                DNS_TYPE_NS = 0x2,
                DNS_TYPE_CNAME = 0x5,
                DNS_TYPE_PTR = 0xC,
                DNS_TYPE_MX = 0xF,
                DNS_TYPE_TXT = 0x10,
                DNS_TYPE_AAAA = 0x1C,
                DNS_TYPE_SRV = 0x21
            }

            private enum DNS_FREE_TYPE
            {
                DnsFreeFlat = 0,
                DnsFreeRecordList = 1,
                DnsFreeParsedMessageFields = 2
            }

            public static IDictionary<string, object>[] QueryDNSForRecordTypeSpecificNameServers(string domainName, IPEndPoint[] dnsServers, DnsRecordTypes recordType)
            {
                if (dnsServers == null || dnsServers.Length == 0)
                {
                    throw new Exception("At least one DNS Server must be provided to do the query");
                }

                IntPtr dnsRequest, addrRequestBuffer, contextBuffer;

                QueryCompletionContext context = new QueryCompletionContext();
                context.eventHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
                context.requestType = recordType;
                context.resultCode = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)));

                List<IDictionary<string, object>> dnsRecords = new List<IDictionary<string, object>>();

                GCHandle handle = GCHandle.Alloc(dnsRecords, GCHandleType.Normal);

                context.dnsRecords = GCHandle.ToIntPtr(handle);

                MakeDnsRequest(domainName, dnsServers, context, out dnsRequest, out addrRequestBuffer, out contextBuffer);

                DNSQueryResult queryResult = new DNSQueryResult();
                queryResult.Version = DnsQueryRequestVersion1;

                IntPtr result = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DNSQueryResult)));
                Marshal.StructureToPtr(queryResult, result, false);

                IntPtr cancelBuffer = Marshal.AllocHGlobal(DNSQueryCancelSize);

                int resCode = DnsQueryEx(dnsRequest, result, cancelBuffer);

                FreeDnsRequest(dnsRequest, addrRequestBuffer, contextBuffer);

                bool requestPending = false;

                switch (resCode)
                {
                    case 0:
                        {
                            queryResult = (DNSQueryResult)Marshal.PtrToStructure(result, typeof(DNSQueryResult));
                            Marshal.FreeHGlobal(result);
                        }

                        break;

                    case DnsRequestPending:
                        {
                            Marshal.FreeHGlobal(result);
                            requestPending = true;
                        }
                        break;

                    case DnsRecordsNoInfo:
                        {
                            Marshal.FreeHGlobal(result);
                            Marshal.FreeHGlobal(cancelBuffer);
                            handle.Free();
                            return new Dictionary<string, object>[0];
                        }

                    default:
                        {
                            Marshal.FreeHGlobal(result);
                            Marshal.FreeHGlobal(cancelBuffer);
                            handle.Free();
                            throw new Win32Exception(resCode);
                        }
                }

                if (!requestPending)
                {
                    Marshal.FreeHGlobal(cancelBuffer);
                    handle.Free();

                    if (queryResult.QueryStatus != 0)
                    {
                        if (queryResult.QueryRecords != IntPtr.Zero)
                        {
                            DnsRecordListFree(queryResult.QueryRecords, DNS_FREE_TYPE.DnsFreeRecordList);
                        }

                        throw new Win32Exception(queryResult.QueryStatus);
                    }

                    dnsRecords.AddRange(ParseRecords(queryResult.QueryRecords, recordType));

                    if (queryResult.QueryRecords != IntPtr.Zero)
                    {
                        DnsRecordListFree(queryResult.QueryRecords, DNS_FREE_TYPE.DnsFreeRecordList);
                    }

                    return dnsRecords.ToArray();
                }

                if (!context.eventHandle.WaitOne(5000))
                {
                    resCode = DnsCancelQuery(cancelBuffer);
                    context.eventHandle.WaitOne();
                    if (resCode != 0)
                    {
                        Marshal.FreeHGlobal(cancelBuffer);
                        handle.Free();
                        throw new Win32Exception(resCode);
                    }
                }

                Marshal.FreeHGlobal(cancelBuffer);

                handle.Free();

                int retCode = Marshal.ReadInt32(context.resultCode);

                Marshal.FreeHGlobal(context.resultCode);

                if (retCode != 0)
                {
                    throw new Win32Exception(retCode);
                }

                return dnsRecords.ToArray();
            }

            private static void FreeDnsRequest(IntPtr requestBuffer, IntPtr addrBuffer, IntPtr contextBuffer)
            {
                if (requestBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(requestBuffer);
                    requestBuffer = IntPtr.Zero;
                }

                if (addrBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(addrBuffer);
                    addrBuffer = IntPtr.Zero;
                }

                if (contextBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(contextBuffer);
                    contextBuffer = IntPtr.Zero;
                }
            }

            private static void MakeDnsRequest(string domainName, IPEndPoint[] dnsServers, QueryCompletionContext context, out IntPtr requestBuffer, out IntPtr addrBuffer, out IntPtr contextBuffer)
            {
                requestBuffer = IntPtr.Zero;
                addrBuffer = IntPtr.Zero;
                contextBuffer = IntPtr.Zero;

                DNS_ADDR[] addrList = new DNS_ADDR[dnsServers.Length];
                int curAddress = 0;

                foreach (var endpoint in dnsServers)
                {
                    addrList[curAddress] = new DNS_ADDR();
                    
                    if (endpoint.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] ipv4AddressBytes = endpoint.Address.GetAddressBytes();

                        SockAddrIn sockAddrIn = new SockAddrIn();

                        Buffer.BlockCopy(ipv4AddressBytes, 0, sockAddrIn.SinAddr, 0, IpAddressV4LengthBytes);

                        sockAddrIn.SinFamily = AFInet;
                        sockAddrIn.SinPort = (ushort)IPAddress.HostToNetworkOrder(endpoint.Port);

                        IntPtr sockAddrInPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SockAddrIn)));
                        Marshal.StructureToPtr(sockAddrIn, sockAddrInPtr, false);

                        Marshal.Copy(sockAddrInPtr, addrList[curAddress].MaxSa, 0, Marshal.SizeOf(typeof(SockAddrIn)));

                        Marshal.FreeHGlobal(sockAddrInPtr);
                    }
                    else if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        SockAddrIn6 sockAddrIn6 = new SockAddrIn6();

                        sockAddrIn6.Sin6Family = AFInet16;
                        sockAddrIn6.Sin6Port = (ushort)IPAddress.HostToNetworkOrder(endpoint.Port);
                        sockAddrIn6.Sin6FlowInfo = 0;

                        byte[] ipv6AddressBytes = endpoint.Address.GetAddressBytes();

                        Buffer.BlockCopy(ipv6AddressBytes, 0, sockAddrIn6.Sin6Addr, 0, IpAddressV6LengthBytes);

                        sockAddrIn6.Sin6ScopeId = 0;

                        IntPtr sockAddrv6InPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SockAddrIn6)));
                        Marshal.StructureToPtr(sockAddrIn6, sockAddrv6InPtr, false);

                        Marshal.Copy(sockAddrv6InPtr, addrList[curAddress].MaxSa, 0, Marshal.SizeOf(typeof(SockAddrIn6)));

                        Marshal.FreeHGlobal(sockAddrv6InPtr);
                    }
                    else
                    {
                        throw new Exception(string.Format("Address family {0} not supported", endpoint.AddressFamily.ToString()));
                    }

                    curAddress++;
                }

                int bufSize = Marshal.SizeOf(typeof(DNS_ADDR_ARRAY)) + (addrList.Length * Marshal.SizeOf(typeof(DNS_ADDR)));

                DNS_ADDR_ARRAY addrArray = new DNS_ADDR_ARRAY();
                addrArray.MaxCount = (uint)dnsServers.Length;
                addrArray.AddrCount = (uint)dnsServers.Length;

                addrBuffer = Marshal.AllocHGlobal(bufSize);
                Marshal.StructureToPtr(addrArray, addrBuffer, false);

                IntPtr addrArrayPointer = new IntPtr(addrBuffer.ToInt64() + Marshal.SizeOf(typeof(DNS_ADDR_ARRAY)));

                for (int i = 0; i < addrList.Length; i++)
                {
                    Marshal.StructureToPtr(addrList[i], addrArrayPointer, false);
                    addrArrayPointer = new IntPtr(addrArrayPointer.ToInt64() + Marshal.SizeOf(typeof(DNS_ADDR)));
                }

                DNS_QUERY_REQUEST request = new DNS_QUERY_REQUEST();
                request.Version = DnsQueryRequestVersion1;
                request.QueryName = domainName;
                request.QueryType = (ushort)context.requestType;
                request.QueryOptions = (ulong)(DnsQueryOptions.DNS_QUERY_BYPASS_CACHE);
                request.DnsServerList = addrBuffer;
                request.InterfaceIndex = 0;
                //request.QueryCompletionCallback = Marshal.GetFunctionPointerForDelegate(new QueryCompletionRoutineFunctionPointer(QueryCompletionRoutine));

                contextBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(context));
                Marshal.StructureToPtr(context, contextBuffer, false);

                request.QueryContext = contextBuffer;

                requestBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(request));
                Marshal.StructureToPtr(request, requestBuffer, false);
            }

            private static void QueryCompletionRoutine(IntPtr queryContext, IntPtr queryResults)
            {
                QueryCompletionContext context = (QueryCompletionContext)Marshal.PtrToStructure(queryContext, typeof(QueryCompletionContext));

                DNSQueryResult queryResult = (DNSQueryResult)Marshal.PtrToStructure(queryResults, typeof(DNSQueryResult));

                Marshal.WriteInt32(context.resultCode, queryResult.QueryStatus);

                if (queryResult.QueryStatus != 0)
                {
                    if (queryResult.QueryRecords != IntPtr.Zero)
                    {
                        DnsRecordListFree(queryResult.QueryRecords, DNS_FREE_TYPE.DnsFreeRecordList);
                    }

                    context.eventHandle.Set();
                    return;
                }

                List<IDictionary<string, object>> records = GCHandle.FromIntPtr(context.dnsRecords).Target as List<IDictionary<string, object>>;

                records.AddRange(ParseRecords(queryResult.QueryRecords, context.requestType));

                if (queryResult.QueryRecords != IntPtr.Zero)
                {
                    DnsRecordListFree(queryResult.QueryRecords, DNS_FREE_TYPE.DnsFreeRecordList);
                }

                context.eventHandle.Set();
            }

            private static IDictionary<string, object>[] ParseRecords(IntPtr result, DnsRecordTypes recordTypeAskedFor)
            {
                DNS_RECORD? record = Marshal.PtrToStructure(result, typeof(DNS_RECORD)) as DNS_RECORD?;

                List<IDictionary<string, object>> records = new List<IDictionary<string, object>>();

                while (record.HasValue)
                {
                    if (record.Value.Type == (ushort)recordTypeAskedFor)
                    {
                        string recordName = Marshal.PtrToStringUni(record.Value.Name);
                        switch (record.Value.Type)
                        {
                            case (ushort)DnsRecordTypes.DNS_TYPE_A:
                                {
                                    string recordValue = ConvertUintToIpAddress(record.Value.Data.A.IpAddress).ToString();
                                    records.Add(new Dictionary<string, object> { { "Type", "A" }, { "Name", recordName }, { "Value", recordValue } });
                                    break;
                                }

                            case (ushort)DnsRecordTypes.DNS_TYPE_MX:
                                {
                                    string nameExchange = Marshal.PtrToStringUni(record.Value.Data.MX.NameExchange);
                                    ushort preference = record.Value.Data.MX.Preference;
                                    records.Add(new Dictionary<string, object> { { "Type", "MX" }, { "Name", recordName }, { "NameExchange", nameExchange }, { "Preference", preference.ToString() } });
                                    break;
                                }

                            case (ushort)DnsRecordTypes.DNS_TYPE_NS:
                            case (ushort)DnsRecordTypes.DNS_TYPE_PTR:
                            case (ushort)DnsRecordTypes.DNS_TYPE_CNAME:
                                {
                                    string type = record.Value.Type == (ushort)DnsRecordTypes.DNS_TYPE_NS ? "NS " :
                                        (record.Value.Type == (ushort)DnsRecordTypes.DNS_TYPE_PTR ? "PTR" : "CNAME");
                                    string nameHost = Marshal.PtrToStringUni(record.Value.Data.PTR.NameHost);
                                    records.Add(new Dictionary<string, object> { { "Type", type }, { "Name", recordName }, { "NameHost", nameHost } });
                                    break;
                                }

                            case (ushort)DnsRecordTypes.DNS_TYPE_TXT:
                                {
                                    IntPtr stringsCursor = record.Value.Data.TXT.StringArray;

                                    StringBuilder valuesBuffer = new StringBuilder();

                                    for (uint i = 0; i < record.Value.Data.TXT.StringCount; i++)
                                    {
                                        IntPtr txtString = Marshal.ReadIntPtr(stringsCursor);

                                        string value = Marshal.PtrToStringUni(stringsCursor);

                                        IDictionary<string, object> txtRecord = new Dictionary<string, object>();
                                        txtRecord.Add("Type", "TXT");
                                        txtRecord.Add("Name", recordName);
                                        txtRecord.Add("Value", value);
                                        records.Add(txtRecord);

                                        valuesBuffer.Append(";" + value);

                                        stringsCursor += Marshal.SizeOf(typeof(IntPtr));
                                    }

                                    break;
                                }

                            case (ushort)DnsRecordTypes.DNS_TYPE_AAAA:
                                {
                                    string recordValue = ConvertAAAAToIpAddress(record.Value.Data.AAAA).ToString();
                                    records.Add(new Dictionary<string, object> { { "Type", "AAAA" }, { "Name", recordName }, { "Value", recordValue } });
                                    break;
                                }

                            case (ushort)DnsRecordTypes.DNS_TYPE_SRV:
                                {
                                    string nameTarget = Marshal.PtrToStringUni(record.Value.Data.SRV.NameTarget);
                                    ushort priority = record.Value.Data.SRV.Priority;
                                    ushort weight = record.Value.Data.SRV.Weight;
                                    ushort port = record.Value.Data.SRV.Port;
                                    records.Add(new Dictionary<string, object> { { "Type", "SRV" }, { "Name", recordName }, { "NameTarget", nameTarget }, { "Priority", priority.ToString() }, { "Weight", weight.ToString() }, { "Port", port.ToString() } });
                                    break;
                                }
                        }
                    }

                    record = Marshal.PtrToStructure(record.Value.Next, typeof(DNS_RECORD)) as DNS_RECORD?;
                }

                return records.ToArray();
            }

            private static IPAddress ConvertUintToIpAddress(uint ipAddress)
            {
                var addressBytes = new byte[4];
                ipAddress = (uint)IPAddress.NetworkToHostOrder((int)ipAddress);
                addressBytes[0] = (byte)((ipAddress & 0xFF000000u) >> 24);
                addressBytes[1] = (byte)((ipAddress & 0x00FF0000u) >> 16);
                addressBytes[2] = (byte)((ipAddress & 0x0000FF00u) >> 8);
                addressBytes[3] = (byte)(ipAddress & 0x000000FFu);
                return new IPAddress(addressBytes);
            }

            private static IPAddress ConvertAAAAToIpAddress(DNS_AAAA_DATA data)
            {
                var addressBytes = new byte[16];
                addressBytes[0] = (byte)(data.Ip6Address0 & 0x000000FF);
                addressBytes[1] = (byte)((data.Ip6Address0 & 0x0000FF00) >> 8);
                addressBytes[2] = (byte)((data.Ip6Address0 & 0x00FF0000) >> 16);
                addressBytes[3] = (byte)((data.Ip6Address0 & 0xFF000000) >> 24);
                addressBytes[4] = (byte)(data.Ip6Address1 & 0x000000FF);
                addressBytes[5] = (byte)((data.Ip6Address1 & 0x0000FF00) >> 8);
                addressBytes[6] = (byte)((data.Ip6Address1 & 0x00FF0000) >> 16);
                addressBytes[7] = (byte)((data.Ip6Address1 & 0xFF000000) >> 24);
                addressBytes[8] = (byte)(data.Ip6Address2 & 0x000000FF);
                addressBytes[9] = (byte)((data.Ip6Address2 & 0x0000FF00) >> 8);
                addressBytes[10] = (byte)((data.Ip6Address2 & 0x00FF0000) >> 16);
                addressBytes[11] = (byte)((data.Ip6Address2 & 0xFF000000) >> 24);
                addressBytes[12] = (byte)(data.Ip6Address3 & 0x000000FF);
                addressBytes[13] = (byte)((data.Ip6Address3 & 0x0000FF00) >> 8);
                addressBytes[14] = (byte)((data.Ip6Address3 & 0x00FF0000) >> 16);
                addressBytes[15] = (byte)((data.Ip6Address3 & 0xFF000000) >> 24);

                return new IPAddress(addressBytes);
            }

            [DllImport("dnsapi", EntryPoint = "DnsQueryEx", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
            private static extern int DnsQueryEx(IntPtr queryRequest, IntPtr queryResults, IntPtr cancelHandle);

            [DllImport("dnsapi", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern void DnsRecordListFree(IntPtr recordList, DNS_FREE_TYPE freeType);

            [DllImport("dnsapi", SetLastError = true)]
            private static extern int DnsCancelQuery(IntPtr cancelHandle);

            [StructLayout(LayoutKind.Sequential)]
            private struct QueryCompletionContext
            {
                public DnsRecordTypes requestType;
                public EventWaitHandle eventHandle;
                public IntPtr dnsRecords;
                public IntPtr resultCode;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_QUERY_REQUEST
            {
                public uint Version;

                [MarshalAs(UnmanagedType.LPWStr)]
                public string QueryName;

                public ushort QueryType;
                public ulong QueryOptions;
                public IntPtr DnsServerList;
                public uint InterfaceIndex;
                public IntPtr QueryCompletionCallback;
                public IntPtr QueryContext;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_ADDR_ARRAY
            {
                public uint MaxCount;
                public uint AddrCount;
                public uint Tag;
                public ushort Family;
                public ushort WordReserved;
                public uint Flags;
                public uint MatchFlag;
                public uint Reserved1;
                public uint Reserved2;
                //// the array of DNS_ADDR follows this
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNSQueryResult
            {
                public uint Version;
                public int QueryStatus;
                public ulong QueryOptions;
                public IntPtr QueryRecords;
                public IntPtr Reserved;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_RECORD
            {
                public IntPtr Next;
                public IntPtr Name;
                public ushort Type;
                public ushort DataLength;
                public FlagsUnion Flags;
                public uint TimeToLive;
                public uint Reserved;
                public DataUnion Data;
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct FlagsUnion
            {
                [FieldOffset(0)]
                public uint DW;

                [FieldOffset(0)]
                public DNS_RECORD_FLAGS S;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_RECORD_FLAGS
            {
                internal uint Data;

                public uint Section
                {
                    get { return this.Data & 0x3u; }
                    set { this.Data = (this.Data & ~0x3u) | (value & 0x3u); }
                }

                public uint Delete
                {
                    get { return (this.Data >> 2) & 0x1u; }
                    set { this.Data = (this.Data & ~(0x1u << 2)) | (value & 0x1u) << 2; }
                }

                public uint CharSet
                {
                    get { return (this.Data >> 3) & 0x3u; }
                    set { this.Data = (this.Data & ~(0x3u << 3)) | (value & 0x3u) << 3; }
                }

                public uint Unused
                {
                    get { return (this.Data >> 5) & 0x7u; }
                    set { this.Data = (this.Data & ~(0x7u << 5)) | (value & 0x7u) << 5; }
                }

                public uint Reserved
                {
                    get { return (this.Data >> 8) & 0xFFFFFFu; }
                    set { this.Data = (this.Data & ~(0xFFFFFFu << 8)) | (value & 0xFFFFFFu) << 8; }
                }
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct DataUnion
            {
                [FieldOffset(0)]
                public DNS_A_DATA A;

                [FieldOffset(0)]
                public DNS_PTR_DATA PTR, NS, CNAME;

                [FieldOffset(0)]
                public DNS_MX_DATA MX;

                [FieldOffset(0)]
                public DNS_TXT_DATA TXT;

                [FieldOffset(0)]
                public DNS_AAAA_DATA AAAA;

                [FieldOffset(0)]
                public DNS_SRV_DATA SRV;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_A_DATA
            {
                public uint IpAddress;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_PTR_DATA
            {
                public IntPtr NameHost;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_MX_DATA
            {
                public IntPtr NameExchange;
                public ushort Preference;
                public ushort Pad;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_TXT_DATA
            {
                public uint StringCount;
                public IntPtr StringArray;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_AAAA_DATA
            {
                public uint Ip6Address0;
                public uint Ip6Address1;
                public uint Ip6Address2;
                public uint Ip6Address3;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DNS_SRV_DATA
            {
                public IntPtr NameTarget;
                public ushort Priority;
                public ushort Weight;
                public ushort Port;
                public ushort Pad;
            }

            [StructLayout(LayoutKind.Sequential)]
            private class DNS_ADDR
            {
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = DnsAddrMaxSockaddrLength)]
                private byte[] maxSa;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
                private uint[] dnsAddrUserDword;

                public DNS_ADDR()
                {
                    this.maxSa = new byte[DnsAddrMaxSockaddrLength];
                    this.dnsAddrUserDword = new uint[8];
                }

                public byte[] MaxSa
                {
                    get
                    {
                        return this.maxSa;
                    }
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            private class SockAddrIn
            {
                private short sinFamily;
                private ushort sinPort;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = IpAddressV4LengthBytes)]
                private byte[] sinAddr;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
                private byte[] sinZero;

                public SockAddrIn()
                {
                    this.sinFamily = 0;
                    this.sinPort = 0;
                    this.sinAddr = new byte[IpAddressV4LengthBytes];
                    this.sinZero = new byte[8];
                }

                public short SinFamily
                {
                    get
                    {
                        return this.sinFamily;
                    }

                    set
                    {
                        this.sinFamily = value;
                    }
                }

                public ushort SinPort
                {
                    get
                    {
                        return this.sinPort;
                    }

                    set
                    {
                        this.sinPort = value;
                    }
                }

                public byte[] SinAddr
                {
                    get
                    {
                        return this.sinAddr;
                    }
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            private class SockAddrIn6
            {
                private short sin6Family;
                private ushort sin6Port;
                private ulong sin6FlowInfo;

                [MarshalAs(UnmanagedType.ByValArray, SizeConst = IpAddressV6LengthBytes)]
                private byte[] sin6Addr;

                private ulong sin6ScopeId;

                public SockAddrIn6()
                {
                    this.sin6Family = AFInet;
                    this.sin6Port = 0;
                    this.sin6Addr = new byte[IpAddressV6LengthBytes];
                    this.sin6ScopeId = 0;
                }

                public short Sin6Family
                {
                    get
                    {
                        return this.sin6Family;
                    }

                    set
                    {
                        this.sin6Family = value;
                    }
                }

                public ushort Sin6Port
                {
                    get
                    {
                        return this.sin6Port;
                    }

                    set
                    {
                        this.sin6Port = value;
                    }
                }

                public ulong Sin6FlowInfo
                {
                    get
                    {
                        return this.sin6FlowInfo;
                    }

                    set
                    {
                        this.sin6FlowInfo = value;
                    }
                }

                public byte[] Sin6Addr
                {
                    get
                    {
                        return this.sin6Addr;
                    }
                }

                public ulong Sin6ScopeId
                {
                    get
                    {
                        return this.sin6ScopeId;
                    }

                    set
                    {
                        this.sin6ScopeId = value;
                    }
                }
            }
        }
    }
}