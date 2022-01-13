﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

#if !NET452

namespace DnsClient.Windows.IpHlpApi
{
    internal class FixedNetworkInformation
    {
        private FixedNetworkInformation()
        {
        }

        public ICollection<IPAddress> DnsAddresses { get; private set; }

        public string DomainName { get; private set; }

        public string HostName { get; private set; }

        public static FixedNetworkInformation GetFixedInformation()
        {
            var info = new FixedNetworkInformation();
            uint size = 0;
            uint result = Interop.IpHlpApi.GetNetworkParams(IntPtr.Zero, ref size);

            while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
            {
                using (var buffer = DisposableIntPtr.Alloc((int)size))
                {
                    if (buffer.IsValid)
                    {
                        result = Interop.IpHlpApi.GetNetworkParams(buffer.Ptr, ref size);

                        Interop.IpHlpApi.FIXED_INFO fixedInfo;
                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            fixedInfo = Marshal.PtrToStructure<Interop.IpHlpApi.FIXED_INFO>(buffer.Ptr);
                        }
                        else
                        {
                            throw new Win32Exception((int)result);
                        }

                        var dnsAddresses = new List<IPAddress>();
                        Interop.IpHlpApi.IP_ADDR_STRING addr = fixedInfo.DnsServerList;

                        if (IPAddress.TryParse(addr.IpAddress, out IPAddress ip))
                        {
                            dnsAddresses.Add(ip);

                            while (addr.Next != IntPtr.Zero)
                            {
                                addr = Marshal.PtrToStructure<Interop.IpHlpApi.IP_ADDR_STRING>(addr.Next);
                                if (IPAddress.TryParse(addr.IpAddress, out ip))
                                {
                                    dnsAddresses.Add(ip);
                                }
                            }
                        }

                        info.HostName = fixedInfo.hostName;
                        info.DomainName = fixedInfo.domainName;
                        info.DnsAddresses = dnsAddresses.ToArray();

                        return info;
                    }
                    else
                    {
                        throw new OutOfMemoryException();
                    }
                }
            }

            return info;
        }
    }
}
#endif
