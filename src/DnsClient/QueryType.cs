// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using DnsClient.Protocol;

namespace DnsClient
{
    /*
     * RFC 1035 (https://tools.ietf.org/html/rfc1035#section-3.2.3)
     * */

    /// <summary>
    /// The query type field appear in the question part of a query.
    /// Query types are a superset of <see cref="ResourceRecordType"/>.
    /// </summary>
    public enum QueryType
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
        A = ResourceRecordType.A,

        /// <summary>
        /// An authoritative name server.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
        /// <seealso cref="NsRecord"/>
        NS = ResourceRecordType.NS,

        /// <summary>
        /// A mail destination (OBSOLETE - use MX).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        [Obsolete("Use MX")]
        MD = ResourceRecordType.MD,

        /// <summary>
        /// A mail forwarder (OBSOLETE - use MX).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        [Obsolete("Use MX")]
        MF = ResourceRecordType.MF,

        /// <summary>
        /// The canonical name for an alias.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.1">RFC 1035</seealso>
        /// <seealso cref="CNameRecord"/>
        CNAME = ResourceRecordType.CNAME,

        /// <summary>
        /// Marks the start of a zone of authority.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.13">RFC 1035</seealso>
        /// <seealso cref="SoaRecord"/>
        SOA = ResourceRecordType.SOA,

        /// <summary>
        /// A mailbox domain name (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.3">RFC 1035</seealso>
        /// <seealso cref="MbRecord"/>
        MB = ResourceRecordType.MB,

        /// <summary>
        /// A mail group member (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.6">RFC 1035</seealso>
        /// <seealso cref="MgRecord"/>
        MG = ResourceRecordType.MG,

        /// <summary>
        /// A mailbox rename domain name (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
        /// <seealso cref="MrRecord"/>
        MR = ResourceRecordType.MR,

        /// <summary>
        /// A Null resource record (EXPERIMENTAL).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
        /// <seealso cref="NullRecord"/>
        NULL = ResourceRecordType.NULL,

        /// <summary>
        /// A well known service description.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3232">RFC 3232</seealso>
        /// <seealso cref="WksRecord"/>
        WKS = ResourceRecordType.WKS,

        /// <summary>
        /// A domain name pointer.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.12">RFC 1035</seealso>
        /// <seealso cref="PtrRecord"/>
        PTR = ResourceRecordType.PTR,

        /// <summary>
        /// Host information.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc1010">RFC 1010</seealso>
        /// <seealso cref="HInfoRecord"/>
        HINFO = ResourceRecordType.HINFO,

        /// <summary>
        /// Mailbox or mail list information.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
        /// <seealso cref="MInfoRecord"/>
        MINFO = ResourceRecordType.MINFO,

        /// <summary>
        /// Mail exchange.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.9">RFC 1035</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc974">RFC 974</seealso>
        /// <seealso cref="MxRecord"/>
        MX = ResourceRecordType.MX,

        /// <summary>
        /// Text resources.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3">RFC 1035</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc1464">RFC 1464</seealso>
        /// <seealso cref="TxtRecord"/>
        TXT = ResourceRecordType.TXT,

        /// <summary>
        /// Responsible Person.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1183">RFC 1183</seealso>
        /// <seealso cref="RpRecord"/>
        RP = ResourceRecordType.RP,

        /// <summary>
        /// AFS Data Base location.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1183#section-1">RFC 1183</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc5864">RFC 5864</seealso>
        /// <seealso cref="AfsDbRecord"/>
        AFSDB = ResourceRecordType.AFSDB,

        /// <summary>
        /// An IPv6 host address.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3596#section-2.2">RFC 3596</seealso>
        /// <seealso cref="AaaaRecord"/>
        AAAA = ResourceRecordType.AAAA,

        /// <summary>
        /// A resource record which specifies the location of the server(s) for a specific protocol and domain.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
        /// <seealso cref="SrvRecord"/>
        SRV = ResourceRecordType.SRV,

        /// <summary>
        /// The Naming Authority Pointer rfc2915
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2915">RFC 2915</seealso>
        /// <seealso cref="NAPtrRecord"/>
        NAPTR = ResourceRecordType.NAPTR,

        /// <summary>
        /// Cryptographic public keys are frequently published, and their
        /// authenticity is demonstrated by certificates.  A CERT resource record
        /// (RR) is defined so that such certificates and related certificate
        /// revocation lists can be stored in the Domain Name System (DNS).
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4398">RFC 4398</seealso>
        /// <seealso cref="CertRecord"/>
        CERT = ResourceRecordType.CERT,

        /// <summary>
        /// DS rfc4034
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#section-5.1">RFC 4034</seealso>
        DS = ResourceRecordType.DS,

        /// <summary>
        /// RRSIG rfc3755.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3755">RFC 3755</seealso>
        RRSIG = ResourceRecordType.RRSIG,

        /// <summary>
        /// NSEC rfc4034.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#section-4">RFC 4034</seealso>
        NSEC = ResourceRecordType.NSEC,

        /// <summary>
        /// DNSKEY rfc4034
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#section-2">RFC 4034</seealso>
        DNSKEY = ResourceRecordType.DNSKEY,

        /// <summary>
        /// NSEC3 rfc5155.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc5155">RFC 5155</seealso>
        NSEC3 = ResourceRecordType.NSEC3,

        /// <summary>
        /// NSEC3PARAM rfc5155.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc5155#section-4">RFC 5155</seealso>
        NSEC3PARAM = ResourceRecordType.NSEC3PARAM,

        /// <summary>
        /// TLSA rfc6698
        /// </summary>
        /// <seealso href="https://https://tools.ietf.org/html/rfc6698">RFC 6698</seealso>
        TLSA = ResourceRecordType.TLSA,

        /// <summary>
        /// TLSA rfc7344.
        /// </summary>
        /// <seealso href="https://https://tools.ietf.org/html/rfc7344">RFC 7344</seealso>
        CDS = ResourceRecordType.CDS,

        /// <summary>
        /// TLSA rfc7344.
        /// </summary>
        /// <seealso href="https://https://tools.ietf.org/html/rfc7344">RFC 7344</seealso>
        CDNS = ResourceRecordType.CDNS,


        /// <summary>
        /// SPF records don't officially have a dedicated RR type, <see cref="ResourceRecordType.TXT"/> should be used instead.
        /// The behavior of TXT and SPF are the same.
        /// </summary>
        /// <remarks>
        /// This library will return a TXT record but will set the header type to SPF if such a record is returned.
        /// </remarks>
        /// <seealso href="https://tools.ietf.org/html/rfc7208">RFC 7208</seealso>
        SPF = ResourceRecordType.SPF,

        /// <summary>
        /// DNS zone transfer request.
        /// This can be used only if <see cref="DnsQuerySettings.UseTcpOnly"/> is set to true as <c>AXFR</c> is only supported via TCP.
        /// <para>
        /// The DNS Server might only return results for the request if the client connection/IP is allowed to do so.
        /// </para>
        /// </summary>
        AXFR = 252,

        /// <summary>
        /// Generic any query *.
        /// </summary>
        ANY = 255,

        /// <summary>
        /// A Uniform Resource Identifier (URI) resource record.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc7553">RFC 7553</seealso>
        /// <seealso cref="UriRecord"/>
        URI = ResourceRecordType.URI,

        /// <summary>
        /// A certification authority authorization.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6844">RFC 6844</seealso>
        /// <seealso cref="CaaRecord"/>
        CAA = ResourceRecordType.CAA,

        /// <summary>
        /// A SSH Fingerprint resource record.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4255">RFC 4255</seealso>
        /// <seealso cref="SshfpRecord"/>
        SSHFP = ResourceRecordType.SSHFP,
    }
}
