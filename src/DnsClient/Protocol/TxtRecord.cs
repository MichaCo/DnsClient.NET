using System;
using System.Collections.Generic;
using System.Linq;

namespace DnsClient.Protocol
{
    /*
    * RFC 1464  https://tools.ietf.org/html/rfc1464

    https://tools.ietf.org/html/rfc1035#section-3.3:
    <character-string> is a single
    length octet followed by that number of characters.  <character-string>
    is treated as binary information, and can be up to 256 characters in
    length (including the length octet).

    https://tools.ietf.org/html/rfc1035#section-3.3.14:
    TXT RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   TXT-DATA                    /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    TXT-DATA        One or more <character-string>s.

    TXT RRs are used to hold descriptive text.  The semantics of the text
    depends on the domain where it is found.
    */

    /// <summary>
    /// TXT RRs are used to hold descriptive text.  The semantics of the text
    /// depends on the domain where it is found.
    /// </summary>
    public class TxtRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the list of TXT values of this TXT RR in escaped form valid for root file.
        /// </summary>
        /// <remarks>
        /// See https://tools.ietf.org/html/rfc1035#section-5.1 for escape details.
        /// </remarks>
        public ICollection<string> EscapedText { get; }

        /// <summary>
        /// Gets the actual UTF8 representation of the text values of this record.
        /// </summary>
        public ICollection<string> Text { get; }

        public TxtRecord(ResourceRecordInfo info, string[] values, string[] utf8Values)
            : base(info)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            if (utf8Values == null)
            {
                throw new ArgumentNullException(nameof(utf8Values));
            }

            EscapedText = values;
            Text = utf8Values;
        }

        public override string RecordToString()
        {
            return string.Join(" ", EscapedText.Select(p => "\"" + p + "\"")).Trim();
        }
    }
}