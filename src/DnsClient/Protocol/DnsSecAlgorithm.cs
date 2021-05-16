namespace DnsClient.Protocol
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores

    public enum DnsSecurityAlgorithm
    {
        None = 0,
        RSAMD5 = 1,
        DH = 2,
        DSA = 3,
        RSASHA1 = 5,
        DSA_NSEC3_SHA1 = 6,
        RSASHA1_NSEC3_SHA1 = 7,
        RSASHA256 = 8,
        RSASHA512 = 10,
        ECCGOST = 12,
        ECDSAP256SHA256 = 13,
        ECDSAP384SHA384 = 14,
        ED25519 = 15,
        ED448 = 16,
        INDIRECT = 252,
        PRIVATEDNS = 253,
        PRIVATEOID = 254
    }

#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}