////using System;
////using System.Collections.Generic;

////namespace DnsClient2
////{
////    /*
////     * Reference RFC6895#section-2.3
////     *
////              RCODE   Name    Description                        Reference
////          Decimal
////            Hexadecimal

////           0    NoError   No Error                           [RFC1035]
////           1    FormErr   Format Error                       [RFC1035]
////           2    ServFail  Server Failure                     [RFC1035]
////           3    NXDomain  Non-Existent Domain                [RFC1035]
////           4    NotImp    Not Implemented                    [RFC1035]
////           5    Refused   Query Refused                      [RFC1035]
////           6    YXDomain  Name Exists when it should not     [RFC2136]
////           7    YXRRSet   RR Set Exists when it should not   [RFC2136]
////           8    NXRRSet   RR Set that should exist does not  [RFC2136]
////           9    NotAuth   Server Not Authoritative for zone  [RFC2136]
////           9    NotAuth   Not Authorized                     [RFC2845]
////          10    NotZone   Name not contained in zone         [RFC2136]

////          11 - 15
////         0xB - 0xF        Unassigned

////          16    BADVERS   Bad OPT Version                    [RFC6891]
////          16    BADSIG    TSIG Signature Failure             [RFC2845]
////          17    BADKEY    Key not recognized                 [RFC2845]
////          18    BADTIME   Signature out of time window       [RFC2845]
////          19    BADMODE   Bad TKEY Mode                      [RFC2930]
////          20    BADNAME   Duplicate key name                 [RFC2930]
////          21    BADALG    Algorithm not supported            [RFC2930]
////          22    BADTRUNC  Bad Truncation                     [RFC4635]

////          23 - 3,840
////      0x0017 - 0x0F00     Unassigned

////       3,841 - 4,095
////      0x0F01 - 0x0FFF     Reserved for Private Use

////       4,096 - 65,534
////      0x1000 - 0xFFFE     Unassigned

////      65,535
////      0xFFFF              Reserved; can only be allocated by Standards
////                          Action.
////    */

////    public enum DnsErrorCode : ushort
////    {
////        NoError = 0,
////        FormErr = 1,
////        ServFail = 2,
////        NXDomain = 3,
////        NotImp = 4,
////        Refused = 5,
////        YXDomain = 6,
////        YXRRSet = 7,
////        NXRRSet = 8,
////        NotAuth = 9,
////        NotZone = 10,
////        BADVERS = 16,   // or BADSIG
////        BADKEY = 17,
////        BADTIME = 18,
////        BADMODE = 19,
////        BADNAME = 20,
////        BADALG = 21,
////        BADTRUNC = 22,
////        BADCOOKIE = 23,
////        Unassigned = 666
////    }

////    public static class DnsErrorCodeText
////    {
////        public const string BADALG = "Algorithm not supported";
////        public const string BADCOOKIE = "Bad/missing Server Cookie";
////        public const string BADKEY = "Key not recognized";
////        public const string BADMODE = "Bad TKEY Mode";
////        public const string BADNAME = "Duplicate key name";
////        public const string BADSIG = "TSIG Signature Failure";
////        public const string BADTIME = "Signature out of time window";
////        public const string BADTRUNC = "Bad Truncation";
////        public const string BADVERS = "Bad OPT Version";
////        public const string FormErr = "Format Error";
////        public const string NoError = "No Error";
////        public const string NotAuth = "Server Not Authoritative for zone or Not Authorized";
////        public const string NotImp = "Not Implemented";
////        public const string NotZone = "Name not contained in zone";
////        public const string NXDomain = "Non-Existent Domain";
////        public const string NXRRSet = "RR Set that should exist does not";
////        public const string Refused = "Query Refused";
////        public const string ServFail = "Server Failure";
////        public const string Unassigned = "Unknown Error";
////        public const string YXDomain = "Name Exists when it should not";
////        public const string YXRRSet = "RR Set Exists when it should not";

////        private static readonly Dictionary<DnsErrorCode, string> errors = new Dictionary<DnsErrorCode, string>()
////        {
////            { DnsErrorCode.NoError, DnsErrorCodeText.NoError },
////            { DnsErrorCode.FormErr, DnsErrorCodeText.FormErr },
////            { DnsErrorCode.ServFail, DnsErrorCodeText.ServFail },
////            { DnsErrorCode.NXDomain, DnsErrorCodeText.NXDomain },
////            { DnsErrorCode.NotImp, DnsErrorCodeText.NotImp },
////            { DnsErrorCode.Refused, DnsErrorCodeText.Refused },
////            { DnsErrorCode.YXDomain, DnsErrorCodeText.YXDomain },
////            { DnsErrorCode.YXRRSet, DnsErrorCodeText.YXRRSet },
////            { DnsErrorCode.NXRRSet, DnsErrorCodeText.NXRRSet },
////            { DnsErrorCode.NotAuth, DnsErrorCodeText.NotAuth },
////            { DnsErrorCode.NotZone, DnsErrorCodeText.NotZone },
////            { DnsErrorCode.BADVERS, DnsErrorCodeText.BADVERS },
////            { DnsErrorCode.BADKEY, DnsErrorCodeText.BADKEY },
////            { DnsErrorCode.BADTIME, DnsErrorCodeText.BADTIME },
////            { DnsErrorCode.BADMODE, DnsErrorCodeText.BADMODE },
////            { DnsErrorCode.BADNAME, DnsErrorCodeText.BADNAME },
////            { DnsErrorCode.BADALG, DnsErrorCodeText.BADALG },
////            { DnsErrorCode.BADTRUNC, DnsErrorCodeText.BADTRUNC },
////            { DnsErrorCode.BADCOOKIE, DnsErrorCodeText.BADCOOKIE },
////        };

////        public static string GetErrorText(DnsErrorCode code)
////        {
////            if (!errors.ContainsKey(code))
////            {
////                return Unassigned;
////            }

////            return errors[code];
////        }
////    }

////    public class DnsErrorException : Exception
////    {
////        public DnsErrorCode Code { get; }

////        public string DnsError { get; }

////        /// <summary>
////        /// Creates an instance of <see cref="DnsErrorException"/> with <see cref="DnsErrorCode.Unassigned"/>.
////        /// </summary>
////        public DnsErrorException() : base(DnsErrorCodeText.Unassigned)
////        {
////            Code = DnsErrorCode.Unassigned;
////            DnsError = DnsErrorCodeText.GetErrorText(Code);
////        }
        
////        /// <summary>
////        /// Creates an instance of <see cref="DnsErrorException"/> with <see cref="DnsErrorCode.Unassigned"/>
////        /// and a custom message.
////        /// </summary>
////        public DnsErrorException(string message) : base(message)
////        {
////            Code = DnsErrorCode.Unassigned;
////            DnsError = DnsErrorCodeText.GetErrorText(Code);
////        }

////        /// <summary>
////        /// Creates an instance of <see cref="DnsErrorException"/> with 
////        /// the standard error text for this <paramref name="code"/>.
////        /// </summary>
////        public DnsErrorException(DnsErrorCode code) : base(DnsErrorCodeText.GetErrorText(code))
////        {
////            Code = code;
////            DnsError = DnsErrorCodeText.GetErrorText(Code);
////        }

////        /// <summary>
////        /// Creates an instance of <see cref="DnsErrorException"/> with <see cref="DnsErrorCode.Unassigned"/>
////        /// and a custom message.
////        /// </summary>
////        public DnsErrorException(string message, Exception innerException) : base(message, innerException)
////        {
////            Code = DnsErrorCode.Unassigned;
////            DnsError = DnsErrorCodeText.GetErrorText(Code);
////        }

////        /// <summary>
////        /// Creates an instance of <see cref="DnsErrorException"/> with a custom message
////        /// and the given <paramref name="code"/>.
////        /// </summary>
////        public DnsErrorException(DnsErrorCode code, string message) : base(message)
////        {
////            Code = code;
////            DnsError = DnsErrorCodeText.GetErrorText(Code);
////        }

////        /// <summary>
////        /// Creates an instance of <see cref="DnsErrorException"/> with a custom message
////        /// and the given <paramref name="code"/>.
////        /// </summary>
////        public DnsErrorException(DnsErrorCode code, string message, Exception innerException) : base(message, innerException)
////        {
////            Code = code;
////            DnsError = DnsErrorCodeText.GetErrorText(Code);
////        }
////    }
////}