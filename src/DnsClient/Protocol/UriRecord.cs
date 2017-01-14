using System;
using System.Linq;

namespace DnsClient.Protocol
{
    /*
    * RFC 7553  https://tools.ietf.org/html/rfc7553

    4.5.  URI RDATA Wire Format

        The RDATA for a URI RR consists of a 2-octet Priority field, a
        2-octet Weight field, and a variable-length Target field.

        Priority and Weight are unsigned integers in network byte order.

        The remaining data in the RDATA contains the Target field.  The
        Target field contains the URI as a sequence of octets (without the
        enclosing double-quote characters used in the presentation format).

        The length of the Target field MUST be greater than zero.

                            1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
        0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |          Priority             |          Weight               |
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        /                                                               /
        /                             Target                            /
        /                                                               /
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    */

    /// <summary>
    /// TXT RRs are used to hold descriptive text.  The semantics of the text
    /// depends on the domain where it is found.
    /// </summary>
    public class UriRecord : DnsResourceRecord
    {
        public string Target { get; set; }

        public int Priority { get; set; }

        public int Weigth { get; set; }

        [CLSCompliant(false)]
        public UriRecord(ResourceRecordInfo info, ushort priority, ushort weight, string target)
            : base(info)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                throw new ArgumentNullException(nameof(target));
            }

            Target = target;
            Priority = priority;
            Weigth = weight;
        }

        public override string RecordToString()
        {
            return $"{Priority} {Weigth} \"{Target}\"";
        }
    }
}