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
    /// X.509 as per PKIX
    /// </summary>
    PKIX = 1,
    /// <summary>
    /// SPKI certificate
    /// </summary>
    SPKI,
    /// <summary>
    /// OpenPGP packet
    /// </summary>
    PGP,
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
    IACPKIK,
    /// <summary>
    /// URI private
    /// </summary>
    URI = 253,
    /// <summary>
    /// OID private
    /// </summary>
    OID = 254
}
