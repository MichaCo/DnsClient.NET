// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
namespace DnsClient.Protocol
{
    /*
     * RFC 1035 (https://tools.ietf.org/html/rfc1035#section-3.2.2)
     * */

    /// <summary>
    /// The resource record types. The <c>enum</c> contains only the types supported by this library at this moment.
    /// The <see cref="ResourceRecordType"/> is used to identify any <see cref="DnsResourceRecord"/>.
    /// <para>
    /// Resource record types are a subset of <see cref="QueryType"/>.
    /// </para>
    /// </summary>
    /// <seealso cref="DnsResourceRecord"/>
    /// <seealso cref="ResourceRecordType"/>
    public enum ResourceRecordType
    {
        /// <summary>
        /// Nothing.
        /// </summary>
        None = 0,

        /// <summary>
        /// A host address.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        /// <seealso cref="ARecord"/>
        A = 1,

        /// <summary>
        /// An authoritative name server.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
        /// <seealso cref="NsRecord"/>
        NS = 2,

        /// <summary>
        /// A mail destination (OBSOLETE - use MX).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        [Obsolete("Use MX")]
        MD = 3,

        /// <summary>
        /// A mail forwarder (OBSOLETE - use MX).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        [Obsolete("Use MX")]
        MF = 4,

        /// <summary>
        /// The canonical name for an alias.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.1">RFC 1035</seealso>
        /// <seealso cref="CNameRecord"/>
        CNAME = 5,

        /// <summary>
        /// Marks the start of a zone of authority.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.13">RFC 1035</seealso>
        /// <seealso cref="SoaRecord"/>
        SOA = 6,

        /// <summary>
        /// A mailbox domain name (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.3">RFC 1035</seealso>
        /// <seealso cref="MbRecord"/>
        MB = 7,

        /// <summary>
        /// A mail group member (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.6">RFC 1035</seealso>
        /// <seealso cref="MgRecord"/>
        MG = 8,

        /// <summary>
        /// A mailbox rename domain name (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
        /// <seealso cref="MrRecord"/>
        MR = 9,

        /// <summary>
        /// A Null resource record (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
        /// <seealso cref="NullRecord"/>
        NULL = 10,

        /// <summary>
        /// A well known service description.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3232">RFC 3232</seealso>
        /// <seealso cref="WksRecord"/>
        WKS = 11,

        /// <summary>
        /// A domain name pointer.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.12">RFC 1035</seealso>
        /// <seealso cref="PtrRecord"/>
        PTR = 12,

        /// <summary>
        /// Host information.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc1010">RFC 1010</seealso>
        /// <seealso cref="HInfoRecord"/>
        HINFO = 13,

        /// <summary>
        /// Mailbox or mail list information.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
        /// <seealso cref="MInfoRecord"/>
        MINFO = 14,

        /// <summary>
        /// Mail exchange.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.9">RFC 1035</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc974">RFC 974</seealso>
        /// <seealso cref="MxRecord"/>
        MX = 15,

        /// <summary>
        /// Text resources.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3">RFC 1035</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc1464">RFC 1464</seealso>
        /// <seealso cref="TxtRecord"/>
        TXT = 16,

        /// <summary>
        /// Responsible Person.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1183">RFC 1183</seealso>
        /// <seealso cref="RpRecord"/>
        RP = 17,

        /// <summary>
        /// AFS Data Base location.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1183#section-1">RFC 1183</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc5864">RFC 5864</seealso>
        /// <seealso cref="AfsDbRecord"/>
        AFSDB = 18,

        /// <summary>
        /// An IPv6 host address.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3596#section-2.2">RFC 3596</seealso>
        /// <seealso cref="AaaaRecord"/>
        AAAA = 28,

        /// <summary>
        /// A resource record which specifies the location of the server(s) for a specific protocol and domain.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
        /// <seealso cref="SrvRecord"/>
        SRV = 33,

        /// <summary>
        /// The Naming Authority Pointer rfc3403
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2915">RFC 2915</seealso>
        /// <seealso cref="NAPtrRecord"/>
        NAPTR = 35,

        /// <summary>
        /// Cryptographic public keys are frequently published, and their
        /// authenticity is demonstrated by certificates.  A CERT resource record
        /// (RR) is defined so that such certificates and related certificate
        /// revocation lists can be stored in the Domain Name System (DNS).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4398">RFC 4398</seealso>
        /// <seealso cref="CertRecord"/>
        CERT = 37,

        /// <summary>
        /// Option record.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6891">RFC 6891</seealso>
        ///// <seealso cref="DnsClient.Protocol.Options.OptRecord"/>
        OPT = 41,

        /// <summary>
        /// DS rfc4034
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#section-5.1">RFC 4034</seealso>
        DS = 43,

        /// <summary>
        /// SSH finger print record.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4255">RFC 4255</seealso>
        SSHFP = 44,

        /// <summary>
        /// RRSIG rfc3755.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3755">RFC 3755</seealso>
        RRSIG = 46,

        /// <summary>
        /// NSEC rfc4034.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#section-4">RFC 4034</seealso>
        NSEC = 47,

        /// <summary>
        /// DNSKEY rfc4034.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#section-2"/>
        DNSKEY = 48,

        /// <summary>
        /// NSEC3 rfc5155.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc5155">RFC 5155</seealso>
        NSEC3 = 50,

        /// <summary>
        /// NSEC3PARAM rfc5155.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc5155#section-4">RFC 5155</seealso>
        NSEC3PARAM = 51,

        /// <summary>
        /// TLSA rfc6698.
        /// </summary>
        /// <seealso href="https://https://tools.ietf.org/html/rfc6698">RFC 6698</seealso>
        TLSA = 52,

        /// <summary>
        /// TLSA rfc7344.
        /// </summary>
        /// <seealso href="https://https://tools.ietf.org/html/rfc7344">RFC 7344</seealso>
        CDS = 59,

        /// <summary>
        /// TLSA rfc7344.
        /// </summary>
        /// <seealso href="https://https://tools.ietf.org/html/rfc7344">RFC 7344</seealso>
        CDNS = 60,



        /// <summary>
        /// SPF records don't officially have a dedicated RR type, <see cref="TXT"/> should be used instead.
        /// The behavior of TXT and SPF are the same.
        /// </summary>
        /// <remarks>
        /// This library will return a TXT record but will set the header type to SPF if such a record is returned.
        /// </remarks>
        /// <seealso href="https://tools.ietf.org/html/rfc7208">RFC 7208</seealso>
        SPF = 99,

        /// <summary>
        /// A Uniform Resource Identifier (URI) resource record.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc7553">RFC 7553</seealso>
        /// <seealso cref="UriRecord"/>
        URI = 256,

        /// <summary>
        /// A certification authority authorization.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6844">RFC 6844</seealso>
        /// <seealso cref="CaaRecord"/>
        CAA = 257,
    }
}
