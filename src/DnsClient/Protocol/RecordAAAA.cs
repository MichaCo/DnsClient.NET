using System.Net;

#region Rfc info
/*
2.2 AAAA data format

   A 128 bit IPv6 address is encoded in the data portion of an AAAA
   resource record in network byte order (high-order byte first).
 */
#endregion

namespace DnsClient.Protocol
{
    public class RecordAAAA : Record
    {
        public IPAddress Address { get; }

        internal RecordAAAA(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            IPAddress address;
            IPAddress.TryParse(
                string.Format("{0:x}:{1:x}:{2:x}:{3:x}:{4:x}:{5:x}:{6:x}:{7:x}",
                recordReader.ReadUInt16(),
                recordReader.ReadUInt16(),
                recordReader.ReadUInt16(),
                recordReader.ReadUInt16(),
                recordReader.ReadUInt16(),
                recordReader.ReadUInt16(),
                recordReader.ReadUInt16(),
                recordReader.ReadUInt16()), out address);

            Address = address;
        }

        public override string ToString()
        {
            return Address.ToString();
        }
    }
}