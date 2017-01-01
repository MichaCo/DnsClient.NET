using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.4.2:
    3.4.2. WKS RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                    ADDRESS                    |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |       PROTOCOL        |                       |
        +--+--+--+--+--+--+--+--+                       |
        |                                               |
        /                   <BIT MAP>                   /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    ADDRESS         An 32 bit Internet address

    PROTOCOL        An 8 bit IP protocol number

    <BIT MAP>       A variable length bit map.  The bit map must be a
                    multiple of 8 bits long.

    The WKS record is used to describe the well known services supported by
    a particular protocol on a particular internet address.  The PROTOCOL
    field specifies an IP protocol number, and the bit map has one bit per
    port of the specified protocol.  The first bit corresponds to port 0,
    the second to port 1, etc.  If the bit map does not include a bit for a
    protocol of interest, that bit is assumed zero.  The appropriate values
    and mnemonics for ports and protocols are specified in [RFC-1010].

    For example, if PROTOCOL=TCP (6), the 26th bit corresponds to TCP port
    25 (SMTP).  If this bit is set, a SMTP server should be listening on TCP
    port 25; if zero, SMTP service is not supported on the specified
    address.

    The purpose of WKS RRs is to provide availability information for
    servers for TCP and UDP.  If a server supports both TCP and UDP, or has
    multiple Internet addresses, then multiple WKS RRs are used.

    WKS RRs cause no additional section processing.

    In master files, both ports and protocols are expressed using mnemonics
    or decimal numbers.

    ** remark:
    * RFS-1010 is obsolete/history.
    * The most current one is https://tools.ietf.org/html/rfc3232
    * The lists of protocols and ports are now handled via the online database on http://www.iana.org/.
    * 
    * Also, see https://tools.ietf.org/html/rfc6335
    * For clarification which protocols are supported etc.
    */
    
    /// <summary>
    /// See http://www.iana.org/assignments/protocol-numbers/protocol-numbers.xhtml.
    /// </summary>
    public class WksRecord : DnsResourceRecord
    {
        public IPAddress Address { get; }

        /// <summary>
        /// Gets the Protocol.
        /// </summary>
        /// <remarks>
        /// According to https://tools.ietf.org/html/rfc6335, only ports for TCP, UDP, DCCP and SCTP services will be assigned.
        /// </remarks>
        public ProtocolType Protocol { get; }

        /// <summary>
        /// Gets the binary raw bitmap.
        /// Use <see cref="Services"/> to determine which ports are actually configured.
        /// </summary>
        public byte[] Bitmap { get; }

        /// <summary>
        /// Gets the list of assigned ports. See http://www.iana.org/assignments/port-numbers.
        /// <para>
        /// For example if this list contains port 25, which is assigned to 
        /// the <c>SMTP</c> service. This means that a SMTP services 
        /// is running on <see cref="Address"/> with transport <see cref="Protocol"/>.
        /// </para>
        /// </summary>
        public int[] Services { get; }

        internal WksRecord(ResourceRecordInfo info, IPAddress address, int protocol, byte[] bitmap)
            : base(info)
        {
            Address = address;
            Protocol = (ProtocolType)protocol;
            Bitmap = bitmap;
            Services = bitmap.GetSetBitsBinary().ToArray();            
        }

        public override string RecordToString()
        {
            return $"{Address} {Protocol} {string.Join(" ", Services)}";
        }
    }

    internal static class BitExtensions
    {
        public static IEnumerable<int> GetSetBitsBinary(this byte[] values)
        {
            for (int vIndex = 0; vIndex < values.Length; vIndex++)
            {
                var bits = values[vIndex].ToBitsBinary().ToArray();
                for (var bIndex = 0; bIndex < bits.Length; bIndex++)
                {
                    if (bits[bIndex])
                    {
                        yield return (vIndex * 8) + bIndex + (8 - bits.Length);
                    }
                }
            }
        }

        public static IEnumerable<KeyValuePair<byte, bool[]>> ToBitsBinary(this byte[] values)
        {
            foreach (var b in values)
            {
                yield return new KeyValuePair<byte, bool[]>(b, b.ToBitsBinary().ToArray());
            }
        }

        public static IEnumerable<bool> ToBitsBinary(this byte value)
        {
            return value.ToBitsBinaryUnordered().Reverse();
        }

        private static IEnumerable<bool> ToBitsBinaryUnordered(this byte value)
        {
            int val = value;
            var radix = 2;
            do
            {
                yield return (val % radix) != 0;
                val = val / radix;

            } while (val != 0);
        }
    }
}