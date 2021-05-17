﻿using System;

namespace DnsClient.Protocol
{
    #region RFC Specifications
    /*
        RFC 2915 : 
        The DNS type code [1] for NAPTR is 35.

       Depending on the value of the
       flags field of the resource record, the resulting domain label or URI
       may be used in subsequent queries for the Naming Authority Pointer
       (NAPTR) resource records (to delegate the name lookup) or as the
       output of the entire process for which this system is used (a
       resolution server for URI resolution, a service URI for ENUM style
       e.164 number to URI mapping, etc).

        Points on the Flag field : RFC 2915, section 2 

          A <character-string> containing flags to control aspects of the
          rewriting and interpretation of the fields in the record.  Flags
          are single characters from the set [A-Z0-9].  The case of the
          alphabetic characters is not significant.

          At this time only four flags, "S", "A", "U", and "P", are
          defined.  The "S", "A" and "U" flags denote a terminal lookup.
          This means that this NAPTR record is the last one and that the
          flag determines what the next stage should be.  The "S" flag
          means that the next lookup should be for SRV records [4].  See
          Section 5 for additional information on how NAPTR uses the SRV
          record type.  "A" means that the next lookup should be for either
          an A, AAAA, or A6 record.  The "U" flag means that the next step
          is not a DNS lookup but that the output of the Regexp field is an
          URI that adheres to the 'absoluteURI' production found in the
          ABNF of RFC 2396 [9].

     */

    /*
    * http://www.faqs.org/rfcs/rfc2915.html
    * 
    8. DNS Packet Format

         The packet format for the NAPTR record is:

                                          1  1  1  1  1  1
            0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          |                     ORDER                     |
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          |                   PREFERENCE                  |
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          /                     FLAGS                     /
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          /                   SERVICES                    /
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          /                    REGEXP                     /
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          /                  REPLACEMENT                  /
          /                                               /
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    Flags 
        A <character-string> which contains various flags.

    Services 
        A <character-string> which contains protocol and service
      identifiers.

    Regexp 
        A <character-string> which contains a regular expression.

    Replacement 
        A <domain-name> which specifies the new value in the
        case where the regular expression is a simple replacement
        operation.

    <character-string> and <domain-name> as used here are defined in
    RFC1035 [1].

    */
    #endregion

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> representing Naming Authority Pointer
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc2915">RFC 2915</seealso>
    [CLSCompliant(false)]
    public class NaptrRecord : DnsResourceRecord
    {
        public const string SIP_SERVICE_KEY = "E2U+SIP";
        public const string EMAIL_SERVICE_KEY = "E2U+EMAIL";
        public const string WEB_SERVICE_KEY = "E2U+WEB";
        public const string SIP_UDP_SERVICE_KEY = "SIP+D2U";
        public const string SIP_TCP_SERVICE_KEY = "SIP+D2T";
        public const string SIPS_TCP_SERVICE_KEY = "SIPS+D2T";
        public const string SIP_WEBSOCKET_SERVICE_KEY = "SIP+D2W";
        public const string SIPS_WEBSOCKET_SERVICE_KEY = "SIPS+D2W";

        public const char A_FLAG = 'A';
        public const char P_FLAG = 'P';
        public const char S_FLAG = 'S';
        public const char U_FLAG = 'U';

        /// <summary>
        /// Gets the Order.
        /// </summary>
        /// <value>
        /// The Order.
        /// </value>
        public ushort Order { get; }
        /// <summary>
        /// Gets the Preference.
        /// </summary>
        /// <value>
        /// The Preference.
        /// </value>
        public ushort Preference { get; }
        /// <summary>
        /// Gets the Flags.
        /// </summary>
        /// <value>
        /// The Flags.
        /// </value>
        public string Flags { get; }
        /// <summary>
        /// Gets the Services.
        /// </summary>
        /// <value>
        /// The Services.
        /// </value>
        public string Services { get; }
        /// <summary>
        /// Gets the Regexp.
        /// </summary>
        /// <value>
        /// The Regexp.
        /// </value>
        public string Regexp { get; }
        /// <summary>
        /// Gets the Replacement.
        /// </summary>
        /// <value>
        /// The Replacement.
        /// </value>
        public DnsString Replacement { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NaptrRecord" /> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="order">The order.</param>
        /// <param name="preference">The preference.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="services">The services.</param>
        /// <param name="regexp">The regexp.</param>
        /// <param name="replacement">The replacement.</param>
        public NaptrRecord(ResourceRecordInfo info, ushort order, ushort preference, string flags, string services, string regexp, DnsString replacement)
            : base(info)
        {
            Order = order;
            Preference = preference;
            Flags = flags;
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Regexp = regexp;
            Replacement = replacement ?? throw new ArgumentNullException(nameof(replacement));
        }

        private protected override string RecordToString()
        {
            return string.Format("{0} {1} \"{2}\" \"{3}\" \"{4}\" {5}",
                Order,
                Preference,
                Flags,
                Services,
                Regexp,
                Replacement);
        }
    }
}
