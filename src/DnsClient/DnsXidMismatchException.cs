using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DnsClient
{
#if !NETSTANDARD1_3
    [Serializable]
#endif

    public class DnsXidMismatchException : Exception
    {
        public int RequestXid { get; }
        public int ResponseXid { get; }

        public DnsXidMismatchException(int requestXid, int responseXid)
            : base()
        {
            RequestXid = requestXid;
            ResponseXid = responseXid;
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
