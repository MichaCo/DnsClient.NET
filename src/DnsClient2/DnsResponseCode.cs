namespace DnsClient2
{
    // <summary>
    /// RFCs 1035, 2136, 2671, 2845, 2930, 4635.
    /// </summary>
    public enum DnsResponseCode : ushort
    {
        /// <summary>
        /// RFC 1035.
        /// No error condition
        /// </summary>
        NoError = 0,

        /// <summary>
        /// RFC 1035.
        /// Format error. The name server was unable to interpret the query.
        /// </summary>
        FormatError = 1,

        /// <summary>
        /// RFC 1035.
        /// Server failure. The name server was unable to process this query due to a problem with the name server.
        /// </summary>
        ServerFailure = 2,

        /// <summary>
        /// RFC 1035.
        /// Name Error. Meaningful only for responses from an authoritative name server, 
        /// this code signifies that the domain name referenced in the query does not exist.
        /// </summary>
        NotExistentDomain = 3,

        /// <summary>
        /// RFC 1035.
        /// Not Implemented. The name server does not support the requested kind of query.
        /// </summary>
        NotImplemented = 4,

        /// <summary>
        /// RFC 1035.
        /// Refused. The name server refuses to perform the specified operation for policy reasons.  
        /// For example, a name server may not wish to provide the information to the particular requester, 
        /// or a name server may not wish to perform a particular operation (e.g., zone transfer) for particular data.
        /// </summary>
        Refused = 5,

        /// <summary>
        /// RFC 2136.
        /// Name Exists when it should not.
        /// </summary>
        ExistingDomain = 6,

        /// <summary>
        /// RFC 2136.
        /// Resource record set exists when it should not.
        /// </summary>
        ExistingResourceRecordSet = 7,

        /// <summary>
        /// RFC 2136.
        /// Resource record set that should exist but does not.
        /// </summary>
        MissingResourceRecordSet = 8,

        /// <summary>
        /// RFC 2136 / RFC2845
        /// Server Not Authoritative for zone / Not Authorized.
        /// </summary>
        NotAuthorized = 9,

        /// <summary>
        /// RFC 2136.
        /// Name not contained in zone.
        /// </summary>
        NotZone = 10,

        /// <summary>
        /// RFCs 2671 / 2845.
        /// Bad OPT Version or TSIG Signature Failure.
        /// </summary>
        BadVersionOrBadSignature = 16,

        /// <summary>
        /// RFC 2845.
        /// Key not recognized.
        /// </summary>
        BadKey = 17,

        /// <summary>
        /// RFC 2845.
        /// Signature out of time window.
        /// </summary>
        BadTime = 18,

        /// <summary>
        /// RFC 2930.
        /// Bad TKEY Mode.
        /// </summary>
        BadMode = 19,

        /// <summary>
        /// RFC 2930.
        /// Duplicate key name.
        /// </summary>
        BadName = 20,

        /// <summary>
        /// RFC 2930.
        /// Algorithm not supported.
        /// </summary>
        BadAlgorithm = 21,

        /// <summary>
        /// RFC 4635.
        /// BADTRUNC - Bad Truncation.
        /// </summary>
        BadTruncation = 22,
    }
}