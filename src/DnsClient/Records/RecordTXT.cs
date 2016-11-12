using System.Collections.Generic;
using System.Text;

#region Rfc info
/*
3.3.14. TXT RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   TXT-DATA                    /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

TXT-DATA        One or more <character-string>s.

TXT RRs are used to hold descriptive text.  The semantics of the text
depends on the domain where it is found.
 * 
*/
#endregion

namespace DnsClient.Records
{
    public class RecordTXT : Record
    {
        public List<string> Text { get; }

        public RecordTXT(ResourceRecord resource, RecordReader recordReader, int length)
            : base(resource)
        {
            int pos = recordReader.Position;
            Text = new List<string>();
            while ((recordReader.Position - pos) < length)
            {
                Text.Add(recordReader.ReadString());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string txt in Text)
            {
                sb.Append(txt);
            }

            return sb.ToString().TrimEnd();
        }
    }
}