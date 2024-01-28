using System;
using System.Collections.Generic;
using System.Linq;

namespace DnsClient.Protocol
{
    /* https://tools.ietf.org/html/rfc4034#section-2
        4.1.  NSEC RDATA Wire Format

           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           /                      Next Domain Name                         /
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           /                       Type Bit Maps                           /
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

        Next Domain Name: The Next Domain field contains the next owner name (in the canonical
        ordering of the zone) that has authoritative data or contains a
        delegation point NS RRset; see Section 6.1 for an explanation of
        canonical ordering.  The value of the Next Domain Name field in the
        last NSEC record in the zone is the name of the zone apex (the owner
        name of the zone's SOA RR).  This indicates that the owner name of
        the NSEC RR is the last name in the canonical ordering of the zone.

        Type Bit Maps: The Type Bit Maps field identifies the RRset types that exist at the
        NSEC RR's owner name.

        The RR type space is split into 256 window blocks, each representing
        the low-order 8 bits of the 16-bit RR type space.  Each block that
        has at least one active RR type is encoded using a single octet
        window number (from 0 to 255), a single octet bitmap length (from 1
        to 32) indicating the number of octets used for the window block's
        bitmap, and up to 32 octets (256 bits) of bitmap.

        Blocks are present in the NSEC RR RDATA in increasing numerical
        order.

            Type Bit Maps Field = ( Window Block # | Bitmap Length | Bitmap )+

            where "|" denotes concatenation.
    */

    /// <summary>
    /// a <see cref="DnsResourceRecord"/> representing a NSEC record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4034#section-2"/>
    public class NSecRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the next owner name (in the canonical ordering of the zone) that has authoritative data
        /// or contains a delegation point NS RRset.
        /// </summary>
        public DnsString NextDomainName { get; }

        /// <summary>
        /// Gets the type bit maps field which identifies the RRSet types that exist at the NSEC RR's owner name.
        /// </summary>
        public IReadOnlyList<byte> TypeBitMapsRaw { get; }

        /// <summary>
        /// Gets the type bit maps field which identifies the RRSet types that exist at the NSEC RR's owner name.
        /// </summary>
        public IReadOnlyList<ResourceRecordType> TypeBitMaps { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NSecRecord"/> class
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/>, <paramref name="nextDomainName"/> or <paramref name="typeBitMaps"/> is null.</exception>
        public NSecRecord(ResourceRecordInfo info, DnsString nextDomainName, byte[] typeBitMaps)
            : base(info)
        {
            NextDomainName = nextDomainName ?? throw new ArgumentNullException(nameof(nextDomainName));
            TypeBitMapsRaw = typeBitMaps ?? throw new ArgumentNullException(nameof(typeBitMaps));
            TypeBitMaps = ReadBitmap(typeBitMaps).OrderBy(p => p).Select(p => (ResourceRecordType)p).ToArray();
        }

        private protected override string RecordToString()
        {
            return string.Format("{0} {1}", NextDomainName, string.Join(" ", TypeBitMaps));
        }

        internal static IEnumerable<int> ReadBitmap(byte[] data)
        {
            if (data.Length < 2)
            {
                yield break;
            }

            for (var n = 0; n < data.Length;)
            {
                var window = data[n++];
                var len = data[n++];

                if (window == 0 && len == 0)
                {
                    continue;
                }

                if (len > 32)
                {
                    throw new DnsResponseParseException("Invalid bitmap length, more than 32 bytes in a window.");
                }

                if (n + len > data.Length)
                {
                    throw new DnsResponseParseException("Invalid bitmap length, more bytes requested then available.");
                }

                for (var k = 0; k < len && n < data.Length; k++)
                {
                    var val = data[n++];

                    for (var bit = 0; bit < 8; ++bit, val >>= 1)
                    {
                        if ((val & 1) == 1)
                        {
                            yield return (7 - bit) + 8 * (k) + 256 * window;
                        }
                    }
                }
            }
        }

        internal static IEnumerable<byte> WriteBitmap(ushort[] values)
        {
            Array.Sort(values);
            int prevValue = -1;

            // max 256 windows à 32bytes possible
            bool[] windowsInUse = new bool[256];
            int maxVal = 0;

            // 32x256 bytes possible
            byte[] typebits = new byte[8192];
            int used = 0;

            foreach (var val in values)
            {
                if (val == prevValue)
                {
                    continue;
                }

                prevValue = val;

                typebits[val / 8] |= (byte)(0x80 >> (val % 8));
                windowsInUse[val / 256] = true;
                if (val > maxVal)
                {
                    maxVal = val;
                }
            }

            for (var block = 0; block <= maxVal / 256; block++)
            {
                var blockLen = 0;
                if (!windowsInUse[block])
                {
                    continue;
                }

                for (var i = 0; i < 32; i++)
                {
                    if (typebits[block * 32 + i] != 0)
                    {
                        blockLen = i + 1;
                    }
                }

                if (blockLen == 0)
                {
                    continue;
                }

                yield return (byte)block;
                yield return (byte)blockLen;
                for (var i = 0; i < blockLen; i++)
                {
                    yield return typebits[block * 32 + i];
                }
                used += blockLen + 2;
            }
        }
    }
}
