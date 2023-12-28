namespace DnsClient.Protocol;

/// <summary>
/// Certificate type values
/// </summary>
/// <remarks>
///  <seealso href="https://tools.ietf.org/html/rfc4398#section-2.1">RFC 4398 section 2.1</seealso>
/// </remarks>
public enum CertificateType
{
    /// <summary>
    /// Reserved certificate type.
    /// </summary>
    Reserved = 0,
    /// <summary>
    /// X509 certificate
    /// </summary>
    X509 = 1,
    /// <summary>
    /// SPKI certificate
    /// </summary>
    SPKI,
    /// <summary>
    /// OpenPGP certificate
    /// </summary>
    PGP,        // Open PGP
    /// <summary>
    /// URL to an X.509 data object 
    /// </summary>
    IPKIX,
    /// <summary>
    ///  Url of an SPKI certificate
    /// </summary>
    ISPKI,
    /// <summary>
    /// fingerprint + URL of an OpenPGP packet
    /// </summary>
    IPGP,
    /// <summary>
    /// Attribute Certificate
    /// </summary>
    ACPKIX,
    /// <summary>
    /// The URL of an Attribute Certificate
    /// </summary>
    IACPKIK
}
