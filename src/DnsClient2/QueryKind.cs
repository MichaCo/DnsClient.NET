using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnsClient2
{
    /*
     * Reference: [RFC6895][RFC1035]
        0	    Query	                            [RFC1035]
        1	    IQuery (Inverse Query, OBSOLETE)	[RFC3425]
        2	    Status	                            [RFC1035]
        3	    Unassigned	
        4	    Notify	                            [RFC1996]
        5	    Update	                            [RFC2136]
        6-15	Unassigned	
     * */
    public enum QueryKind
    {
        Query,
        IQuery,
        Status,
        Unassinged3,
        Notify,
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
