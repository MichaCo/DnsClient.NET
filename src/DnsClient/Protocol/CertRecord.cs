using System;
using System.Collections.Generic;
using System.Text;

namespace DnsClient.Protocol;


/// <summary>A representation of CERT RDATA format.
/// <remarks>
/// RFC 4398.
///
/// Record format:
/// <code>
///                     1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
/// 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |             type              |             key tag           |
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
/// |   algorithm   |                                               /
/// +---------------+            certificate or CRL                 /
/// /                                                               /
/// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-|
/// </code>
/// </remarks>
/// <see cref="https://datatracker.ietf.org/doc/html/rfc4398#section-2"/>
/// </summary>
public class CertRecord : DnsResourceRecord
{
    /// <summary>
    /// Gets the <see cref="CertType"/> referred to by this record
    /// </summary>
    /// <value>The CERT RR type of this certificate</value>
    public CertificateType CertType { get; }

    /// <summary>
    /// Gets the key tag value of the <see cref="DnsKeyRecord"/> referred to by this record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4034#appendix-B">Key Tag Calculation</seealso>
    public int KeyTag { get; }

    /// <summary>
    /// Gets certificate algorithm (see RFC 4034, Appendix 1)
    /// </summary>
    public DnsSecurityAlgorithm Algorithm { get; }

    /// <summary>
    /// Gets the raw certificate RDATA.
    /// </summary>
    /// <summary>
    /// Gets the public key material.
    /// The format depends on the <see cref="Algorithm"/> of the key being stored.
    /// </summary>
    public IReadOnlyList<byte> PublicKey { get; }

    /// <summary>
    /// Gets the base64 string representation of the <see cref="PublicKey"/>.
    /// </summary>
    public string PublicKeyAsString { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsKeyRecord"/> class
    /// </summary>
    /// <param name="info"></param>
    /// <param name="flags"></param>
    /// <param name="protocol"></param>
    /// <param name="algorithm"></param>
    /// <param name="publicKey"></param>
    /// <exception cref="ArgumentNullException">If <paramref name="info"/> or <paramref name="publicKey"/> is null.</exception>
    public CertRecord(ResourceRecordInfo info, int certType, int keyTag, byte algorithm, byte[] publicKey)
        : base(info)
    {
        CertType = (CertificateType)certType;
        KeyTag = keyTag;
        Algorithm = (DnsSecurityAlgorithm)algorithm;
        PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        PublicKeyAsString = Convert.ToBase64String(publicKey);
    }

    
    private protected override string RecordToString()
    {
        return string.Format("{0} {1} {2} {3}", CertType, KeyTag, Algorithm, PublicKeyAsString);
    }
}
