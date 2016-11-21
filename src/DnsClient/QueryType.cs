using System;
using System.Linq;
namespace DnsClient
{
    /*
     * RFC 1035 (https://tools.ietf.org/html/rfc1035#section-3.2.3)
     * */

    /// <summary>
    /// QTYPE fields appear in the question part of a query.  QTYPES are a superset of TYPEs, hence all TYPEs are valid QTYPEs.
    /// </summary>
    public enum QueryType : short
    {
        /// <summary>
        /// A host address  [RFC1035].
        /// </summary>
        A = 1,

        /// <summary>
        /// An authoritative name server    [RFC1035].
        /// </summary>
        NS = 2,

        /// <summary>
        /// A mail destination (OBSOLETE - use MX)	[RFC1035].
        /// </summary>
        MD = 3,

        /// <summary>
        /// A mail forwarder (OBSOLETE - use MX)	[RFC1035].
        /// </summary>
        MF = 4,

        /// <summary>
        /// The canonical name for an alias [RFC1035].
        /// </summary>
        CNAME = 5,

        /// <summary>
        /// Marks the start of a zone of authority  [RFC1035].
        /// </summary>
        SOA = 6,

        /// <summary>
        /// A mailbox domain name (EXPERIMENTAL)	[RFC1035].  TODO:impl
        /// </summary>
        MB = 7,

        /// <summary>
        /// A mail group member (EXPERIMENTAL)	[RFC1035].  TODO:impl
        /// </summary>
        MG = 8,

        /// <summary>
        /// A mail rename domain name (EXPERIMENTAL)	[RFC1035].  TODO:impl
        /// </summary>
        MR = 9,

        /// <summary>
        /// A null RR (EXPERIMENTAL)	[RFC1035].  TODO:impl
        /// </summary>
        NULL = 10,

        /// <summary>
        /// A well known service description    [RFC1035]   TODO:impl
        /// </summary>
        WKS = 11,

        /// <summary>
        /// A domain name pointer   [RFC1035]
        /// </summary>
        PTR = 12,

        /// <summary>
        /// Host information    [RFC1035]   TODO:impl
        /// </summary>
        HINFO = 13,

        /// <summary>
        /// Mailbox or mail list information    [RFC1035]   TODO:impl
        /// </summary>
        MINFO = 14,

        /// <summary>
        /// Mail exchange   [RFC1035]
        /// </summary>
        MX = 15,

        /// <summary>
        /// Text strings    [RFC1035]
        /// </summary>
        TXT = 16,

        /// <summary>
        /// A IPV6 host address, [RFC3596]
        /// </summary>
        AAAA = 28,

        /// <summary>
        /// Location of services [RFC2782]
        /// </summary>
        SRV = 33,

        /// <summary>
        /// RRSIG rfc3755.  TODO:impl
        /// </summary>
        RRSIG = 46,

        /// <summary>
        /// Generic any query *.
        /// </summary>
        ANY = 255,

        CAA = 257,
    }
}