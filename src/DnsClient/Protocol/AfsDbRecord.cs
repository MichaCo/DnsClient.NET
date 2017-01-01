using System;
using System.Net;

namespace DnsClient.Protocol
{
    /* https://tools.ietf.org/html/rfc1183#section-1
    1. AFS Data Base location

       This section defines an extension of the DNS to locate servers both
       for AFS (AFS is a registered trademark of Transarc Corporation) and
       for the Open Software Foundation's (OSF) Distributed Computing
       Environment (DCE) authenticated naming system using HP/Apollo's NCA,
       both to be components of the OSF DCE.  The discussion assumes that
       the reader is familiar with AFS [5] and NCA [6].

       The AFS (originally the Andrew File System) system uses the DNS to
       map from a domain name to the name of an AFS cell database server.
       The DCE Naming service uses the DNS for a similar function: mapping
       from the domain name of a cell to authenticated name servers for that
       cell.  The method uses a new RR type with mnemonic AFSDB and type
       code of 18 (decimal).

       AFSDB has the following format:

       <owner> <ttl> <class> AFSDB <subtype> <hostname>
    */
    
    public class AfsDbRecord : DnsResourceRecord
    {
        public AfsType SubType { get; }

        public DnsName Hostname { get; }

        public AfsDbRecord(ResourceRecordInfo info, AfsType type, DnsName name) : base(info)
        {
            SubType = type;
            Hostname = name;
        }

        public override string RecordToString()
        {
            return $"{(int)SubType} {Hostname}";
        }
    }

    public enum AfsType
    {
        Afs = 1,
        Dce = 2
    }
}