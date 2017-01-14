using System;

namespace DnsClient.Protocol
{
    /*
    3.3.9. MX RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                  PREFERENCE                   |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   EXCHANGE                    /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    PREFERENCE      A 16 bit integer which specifies the preference given to
                    this RR among others at the same owner.  Lower values
                    are preferred.

    EXCHANGE        A <domain-name> which specifies a host willing to act as
                    a mail exchange for the owner name.

    MX records cause type A additional section processing for the host
    specified by EXCHANGE.  The use of MX RRs is explained in detail in
    [RFC-974].
    */

    /// <summary>
    /// MX records cause type A additional section processing for the host
    /// specified by EXCHANGE.The use of MX RRs is explained in detail in
    /// [RFC-974].
    /// </summary>
    [CLSCompliant(false)]
    public class MxRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets a 16 bit integer which specifies the preference given to
        /// this RR among others at the same owner.
        /// Lower values are preferred.
        /// </summary>
        public ushort Preference { get; }

        /// <summary>
        /// A <domain-name> which specifies a host willing to act as a mail exchange.
        /// </summary>
        public DnsString Exchange { get; }

        public MxRecord(ResourceRecordInfo info, ushort preference, DnsString domainName)
            : base(info)
        {
            if (domainName == null)
            {
                throw new ArgumentNullException(nameof(domainName));
            }

            Preference = preference;
            Exchange = domainName;
        }

        public override string RecordToString()
        {
            return string.Format("{0} {1}", Preference, Exchange);
        }
    }
}