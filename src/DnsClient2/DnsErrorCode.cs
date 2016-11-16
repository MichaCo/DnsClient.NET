using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnsClient2
{
    /*
     * Reference RFC6895#section-2.3
     * 
              RCODE   Name    Description                        Reference
          Decimal
            Hexadecimal

           0    NoError   No Error                           [RFC1035]
           1    FormErr   Format Error                       [RFC1035]
           2    ServFail  Server Failure                     [RFC1035]
           3    NXDomain  Non-Existent Domain                [RFC1035]
           4    NotImp    Not Implemented                    [RFC1035]
           5    Refused   Query Refused                      [RFC1035]
           6    YXDomain  Name Exists when it should not     [RFC2136]
           7    YXRRSet   RR Set Exists when it should not   [RFC2136]
           8    NXRRSet   RR Set that should exist does not  [RFC2136]
           9    NotAuth   Server Not Authoritative for zone  [RFC2136]
           9    NotAuth   Not Authorized                     [RFC2845]
          10    NotZone   Name not contained in zone         [RFC2136]

          11 - 15
         0xB - 0xF        Unassigned

          16    BADVERS   Bad OPT Version                    [RFC6891]
          16    BADSIG    TSIG Signature Failure             [RFC2845]
          17    BADKEY    Key not recognized                 [RFC2845]
          18    BADTIME   Signature out of time window       [RFC2845]
          19    BADMODE   Bad TKEY Mode                      [RFC2930]
          20    BADNAME   Duplicate key name                 [RFC2930]
          21    BADALG    Algorithm not supported            [RFC2930]
          22    BADTRUNC  Bad Truncation                     [RFC4635]

          23 - 3,840
      0x0017 - 0x0F00     Unassigned

       3,841 - 4,095
      0x0F01 - 0x0FFF     Reserved for Private Use

       4,096 - 65,534
      0x1000 - 0xFFFE     Unassigned

      65,535
      0xFFFF              Reserved; can only be allocated by Standards
                          Action. 
    */
    public enum DnsErrorCode
    {
    }
}
