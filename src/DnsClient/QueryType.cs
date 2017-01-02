using System;
using System.Linq;
using DnsClient.Protocol;

namespace DnsClient
{
    /*
     * RFC 1035 (https://tools.ietf.org/html/rfc1035#section-3.2.3)
     * */

    /// <summary>
    /// The query type field appear in the question part of a query.
    /// Query types are a superset of <see cref="Protocol.ResourceRecordType"/>.
    /// </summary>
    public enum QueryType : short
    {
        /// <summary>
        /// A host address  [RFC1035].
        /// </summary>
        A = ResourceRecordType.A,

        /// <summary>
        /// An authoritative name server    [RFC1035].
        /// </summary>
        NS = ResourceRecordType.NS,

        /// <summary>
        /// A mail destination (OBSOLETE - use MX)	[RFC1035].
        /// </summary>
        [Obsolete("Use MX")]
        MD = 3,

        /// <summary>
        /// A mail forwarder (OBSOLETE - use MX)	[RFC1035].
        /// </summary>
        [Obsolete("Use MX")]
        MF = 4,

        /// <summary>
        /// The canonical name for an alias [RFC1035].
        /// </summary>
        CNAME = ResourceRecordType.CNAME,

        /// <summary>
        /// Marks the start of a zone of authority  [RFC1035].
        /// </summary>
        SOA = ResourceRecordType.SOA,

        /// <summary>
        /// A mailbox domain name (EXPERIMENTAL)	[RFC1035].
        /// </summary>
        MB = ResourceRecordType.MB,

        /// <summary>
        /// A mail group member (EXPERIMENTAL)	[RFC1035].
        /// </summary>
        MG = ResourceRecordType.MG,

        /// <summary>
        /// A mail rename domain name (EXPERIMENTAL)	[RFC1035].
        /// </summary>
        MR = ResourceRecordType.MR,

        /// <summary>
        /// A null RR (EXPERIMENTAL)	[RFC1035].
        /// </summary>
        NULL = ResourceRecordType.NULL,

        /// <summary>
        /// A well known service description    [RFC1035]
        /// </summary>
        WKS = ResourceRecordType.WKS,

        /// <summary>
        /// A domain name pointer   [RFC1035]
        /// </summary>
        PTR = ResourceRecordType.PTR,

        /// <summary>
        /// Host information    [RFC1035]
        /// </summary>
        HINFO = ResourceRecordType.HINFO,

        /// <summary>
        /// Mailbox or mail list information    [RFC1035]
        /// </summary>
        MINFO = ResourceRecordType.MINFO,

        /// <summary>
        /// Mail exchange   [RFC1035]
        /// </summary>
        MX = ResourceRecordType.MX,

        /// <summary>
        /// Text strings    [RFC1035]
        /// </summary>
        TXT = ResourceRecordType.TXT,

        /// <summary>
        /// A IPV6 host address, [RFC3596]
        /// </summary>
        AAAA = ResourceRecordType.AAAA,

        /// <summary>
        /// Location of services [RFC2782]
        /// </summary>
        SRV = ResourceRecordType.SRV,

        /// <summary>
        /// RRSIG rfc3755.
        /// </summary>
        RRSIG = ResourceRecordType.RRSIG,

        /// <summary>
        /// Generic any query *.
        /// </summary>
        ANY = 255,

        CAA = ResourceRecordType.CAA,
    }
}