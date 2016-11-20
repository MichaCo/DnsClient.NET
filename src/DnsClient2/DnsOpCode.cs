using System;

namespace DnsClient2
{
    /*
     *
     * Reference: [RFC6895][RFC1035]
        0	    Query	                            [RFC1035]
        1	    IQuery (Inverse Query, OBSOLETE)	[RFC3425]
        2	    Status	                            [RFC1035]
        3	    Unassigned
        4	    Notify	                            [RFC1996]
        5	    Update	                            [RFC2136]
        6-15	Unassigned
     * */

    /// <summary>
    /// RFCs 1035, 1996, 2136, 3425.
    /// Specifies kind of query in this message.
    /// This value is set by the originator of a query and copied into the response.
    /// </summary>
    public enum DnsOpCode : ushort
    {
        /// <summary>
        /// RFC 1035.
        /// A standard query.
        /// </summary>
        Query,

        /// <summary>
        /// RFC 3425.
        /// An inverse query.
        /// </summary>
        [Obsolete]
        IQuery,

        /// <summary>
        /// RFC 1035.
        /// A server status request.
        /// </summary>
        Status,

        Unassinged3,

        /// <summary>
        /// RFC 1996.
        /// </summary>
        Notify,

        /// <summary>
        /// RFC 2136.
        /// </summary>
        Update,

        Unassinged6,
        Unassinged7,
        Unassinged8,
        Unassinged9,
        Unassinged10,
        Unassinged11,
        Unassinged12,
        Unassinged13,
        Unassinged14,
        Unassinged15,
    }
}