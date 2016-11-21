using System;
using System.Collections.Generic;

namespace DnsClient2
{
    /*
     * Reference RFC6895#section-2.3
     */
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

        /// <summary>
        /// RFC 7873
        /// Bad/missing Server Cookie
        /// </summary>
        BadCookie = 23,

        /// <summary>
        /// Unknown error.
        /// </summary>
        Unassigned = 666
    }


    public static class DnsResponseCodeText
    {
        internal const string BADALG = "Algorithm not supported";
        internal const string BADCOOKIE = "Bad/missing Server Cookie";
        internal const string BADKEY = "Key not recognized";
        internal const string BADMODE = "Bad TKEY Mode";
        internal const string BADNAME = "Duplicate key name";
        internal const string BADSIG = "TSIG Signature Failure";
        internal const string BADTIME = "Signature out of time window";
        internal const string BADTRUNC = "Bad Truncation";
        internal const string BADVERS = "Bad OPT Version";
        internal const string FormErr = "Format Error";
        internal const string NoError = "No Error";
        internal const string NotAuth = "Server Not Authoritative for zone or Not Authorized";
        internal const string NotImp = "Not Implemented";
        internal const string NotZone = "Name not contained in zone";
        internal const string NXDomain = "Non-Existent Domain";
        internal const string NXRRSet = "RR Set that should exist does not";
        internal const string Refused = "Query Refused";
        internal const string ServFail = "Server Failure";
        internal const string Unassigned = "Unknown Error";
        internal const string YXDomain = "Name Exists when it should not";
        internal const string YXRRSet = "RR Set Exists when it should not";

        private static readonly Dictionary<DnsResponseCode, string> errors = new Dictionary<DnsResponseCode, string>()
        {
            { DnsResponseCode.NoError, DnsResponseCodeText.NoError },
            { DnsResponseCode.FormatError, DnsResponseCodeText.FormErr },
            { DnsResponseCode.ServerFailure, DnsResponseCodeText.ServFail },
            { DnsResponseCode.NotExistentDomain, DnsResponseCodeText.NXDomain },
            { DnsResponseCode.NotImplemented, DnsResponseCodeText.NotImp },
            { DnsResponseCode.Refused, DnsResponseCodeText.Refused },
            { DnsResponseCode.ExistingDomain, DnsResponseCodeText.YXDomain },
            { DnsResponseCode.ExistingResourceRecordSet, DnsResponseCodeText.YXRRSet },
            { DnsResponseCode.MissingResourceRecordSet, DnsResponseCodeText.NXRRSet },
            { DnsResponseCode.NotAuthorized, DnsResponseCodeText.NotAuth },
            { DnsResponseCode.NotZone, DnsResponseCodeText.NotZone },
            { DnsResponseCode.BadVersionOrBadSignature, DnsResponseCodeText.BADVERS },
            { DnsResponseCode.BadKey, DnsResponseCodeText.BADKEY },
            { DnsResponseCode.BadTime, DnsResponseCodeText.BADTIME },
            { DnsResponseCode.BadMode, DnsResponseCodeText.BADMODE },
            { DnsResponseCode.BadName, DnsResponseCodeText.BADNAME },
            { DnsResponseCode.BadAlgorithm, DnsResponseCodeText.BADALG },
            { DnsResponseCode.BadTruncation, DnsResponseCodeText.BADTRUNC },
            { DnsResponseCode.BadCookie, DnsResponseCodeText.BADCOOKIE },
        };

        public static string GetErrorText(DnsResponseCode code)
        {
            if (!errors.ContainsKey(code))
            {
                return Unassigned;
            }

            return errors[code];
        }
    }

    public class DnsResponseException : Exception
    {
        public DnsResponseCode Code { get; }

        public string DnsError { get; }

        /// <summary>
        /// Creates an instance of <see cref="DnsResponseException"/> with <see cref="DnsResponseCode.Unassigned"/>.
        /// </summary>
        public DnsResponseException() : base(DnsResponseCodeText.Unassigned)
        {
            Code = DnsResponseCode.Unassigned;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Creates an instance of <see cref="DnsResponseException"/> with <see cref="DnsResponseCode.Unassigned"/>
        /// and a custom message.
        /// </summary>
        public DnsResponseException(string message) : base(message)
        {
            Code = DnsResponseCode.Unassigned;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Creates an instance of <see cref="DnsResponseException"/> with 
        /// the standard error text for this <paramref name="code"/>.
        /// </summary>
        public DnsResponseException(DnsResponseCode code) : base(DnsResponseCodeText.GetErrorText(code))
        {
            Code = code;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Creates an instance of <see cref="DnsResponseException"/> with <see cref="DnsResponseCode.Unassigned"/>
        /// and a custom message.
        /// </summary>
        public DnsResponseException(string message, Exception innerException) : base(message, innerException)
        {
            Code = DnsResponseCode.Unassigned;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Creates an instance of <see cref="DnsResponseException"/> with a custom message
        /// and the given <paramref name="code"/>.
        /// </summary>
        public DnsResponseException(DnsResponseCode code, string message) : base(message)
        {
            Code = code;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }

        /// <summary>
        /// Creates an instance of <see cref="DnsResponseException"/> with a custom message
        /// and the given <paramref name="code"/>.
        /// </summary>
        public DnsResponseException(DnsResponseCode code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
            DnsError = DnsResponseCodeText.GetErrorText(Code);
        }
    }
}