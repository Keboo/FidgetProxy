using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;

namespace Keboo.Web.Proxy.Network.Certificate;

/// <inheritdoc />
/// <summary>
///     Certificate Maker - uses MakeCert
///     Calls COM objects using reflection
/// </summary>
[SupportedOSPlatform("windows")]
internal class WinCertificateMaker : ICertificateMaker
{
    private readonly string sProviderName = "Microsoft Enhanced Cryptographic Provider v1.0";

    private readonly Type _typeAltNamesCollection;

    private readonly Type _typeBasicConstraints;

    private readonly Type _typeCAlternativeName;

    private readonly Type _typeEkuExt;

    private readonly Type _typeExtNames;

    private readonly Type _typeKuExt;

    private readonly Type _typeOid;

    private readonly Type _typeOids;

    private readonly Type _typeRequestCert;

    private readonly Type _typeSignerCertificate;
    private readonly Type _typeX500Dn;

    private readonly Type _typeX509Enrollment;

    private readonly Type _typeX509Extensions;

    private readonly Type _typeX509PrivateKey;

    // Validity Days for Root Certificates Generated.
    private readonly int _certificateValidDays;

    private object? _sharedPrivateKey;

    /// <summary>
    ///     Constructor.
    /// </summary>
    internal WinCertificateMaker(int certificateValidDays)
    {
        _certificateValidDays = certificateValidDays;

        _typeX500Dn = GetType("X509Enrollment.CX500DistinguishedName", true);
        _typeX509PrivateKey = GetType("X509Enrollment.CX509PrivateKey", true);
        _typeOid = GetType("X509Enrollment.CObjectId", true);
        _typeOids = GetType("X509Enrollment.CObjectIds.1", true);
        _typeEkuExt = GetType("X509Enrollment.CX509ExtensionEnhancedKeyUsage");
        _typeKuExt = GetType("X509Enrollment.CX509ExtensionKeyUsage");
        _typeRequestCert = GetType("X509Enrollment.CX509CertificateRequestCertificate");
        _typeX509Extensions = GetType("X509Enrollment.CX509Extensions");
        _typeBasicConstraints = GetType("X509Enrollment.CX509ExtensionBasicConstraints");
        _typeSignerCertificate = GetType("X509Enrollment.CSignerCertificate");
        _typeX509Enrollment = GetType("X509Enrollment.CX509Enrollment");

        // for alternative names
        _typeAltNamesCollection = GetType("X509Enrollment.CAlternativeNames");
        _typeExtNames = GetType("X509Enrollment.CX509ExtensionAlternativeNames");
        _typeCAlternativeName = GetType("X509Enrollment.CAlternativeName");

        static Type GetType(string programId, bool throwOnError = false)
            => Type.GetTypeFromProgID(programId, throwOnError) ??
               throw new InvalidOperationException($"Could not retrieve {programId}");
    }


    /// <summary>
    ///     Make certificate.
    /// </summary>
    public X509Certificate2 MakeCertificate(string sSubjectCn, X509Certificate2? signingCert = null)
    {
        return MakeCertificate(sSubjectCn, true, signingCert);
    }

    private X509Certificate2 MakeCertificate(string sSubjectCn,
        bool switchToMtaIfNeeded, X509Certificate2? signingCertificate = null,
        CancellationToken cancellationToken = default)
    {
        if (switchToMtaIfNeeded && Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
            return Task.Run(() => MakeCertificate(sSubjectCn, false, signingCertificate),
                cancellationToken).Result;

        // Subject
        var fullSubject = $"CN={sSubjectCn}";

        // Sig Algo
        const string hashAlgo = "SHA256";

        // Grace Days
        const int graceDays = -366;

        // KeyLength
        const int keyLength = 2048;

        var now = DateTime.UtcNow;
        var graceTime = now.AddDays(graceDays);
        var certificate = MakeCertificate(sSubjectCn, fullSubject, keyLength, hashAlgo, graceTime,
            now.AddDays(_certificateValidDays), signingCertificate);
        return certificate;
    }

    private X509Certificate2 MakeCertificate(string subject, string fullSubject,
        int privateKeyLength, string hashAlg, DateTime validFrom, DateTime validTo,
        X509Certificate2? signingCertificate)
    {
        var x500CertDn = Activator.CreateInstance(_typeX500Dn);
        object?[] typeValue = [fullSubject, 0];
        _typeX500Dn.InvokeMember("Encode", BindingFlags.InvokeMethod, null, x500CertDn, typeValue);

        var x500RootCertDn = Activator.CreateInstance(_typeX500Dn);

        if (signingCertificate != null) typeValue[0] = signingCertificate.Subject;

        _typeX500Dn.InvokeMember("Encode", BindingFlags.InvokeMethod, null, x500RootCertDn, typeValue);

        object? sharedPrivateKey = null;
        if (signingCertificate != null) sharedPrivateKey = this._sharedPrivateKey;

        if (sharedPrivateKey == null)
        {
            sharedPrivateKey = Activator.CreateInstance(_typeX509PrivateKey);
            typeValue = [sProviderName];
            _typeX509PrivateKey.InvokeMember("ProviderName", BindingFlags.PutDispProperty, null, sharedPrivateKey,
                typeValue);
            typeValue[0] = 2;
            _typeX509PrivateKey.InvokeMember("ExportPolicy", BindingFlags.PutDispProperty, null, sharedPrivateKey,
                typeValue);
            typeValue = [signingCertificate == null ? 2 : 1];
            _typeX509PrivateKey.InvokeMember("KeySpec", BindingFlags.PutDispProperty, null, sharedPrivateKey,
                typeValue);

            if (signingCertificate != null)
            {
                typeValue = [176];
                _typeX509PrivateKey.InvokeMember("KeyUsage", BindingFlags.PutDispProperty, null, sharedPrivateKey,
                    typeValue);
            }

            typeValue[0] = privateKeyLength;
            _typeX509PrivateKey.InvokeMember("Length", BindingFlags.PutDispProperty, null, sharedPrivateKey,
                typeValue);
            _typeX509PrivateKey.InvokeMember("Create", BindingFlags.InvokeMethod, null, sharedPrivateKey, null);

            if (signingCertificate != null) this._sharedPrivateKey = sharedPrivateKey;
        }

        typeValue = new object[1];

        var oid = Activator.CreateInstance(_typeOid);
        typeValue[0] = "1.3.6.1.5.5.7.3.1";
        _typeOid.InvokeMember("InitializeFromValue", BindingFlags.InvokeMethod, null, oid, typeValue);

        var oids = Activator.CreateInstance(_typeOids);
        typeValue[0] = oid;
        _typeOids.InvokeMember("Add", BindingFlags.InvokeMethod, null, oids, typeValue);

        var ekuExt = Activator.CreateInstance(_typeEkuExt);
        typeValue[0] = oids;
        _typeEkuExt.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, ekuExt, typeValue);

        var requestCert = Activator.CreateInstance(_typeRequestCert);

        typeValue = new[] { 1, sharedPrivateKey, string.Empty };
        _typeRequestCert.InvokeMember("InitializeFromPrivateKey", BindingFlags.InvokeMethod, null, requestCert,
            typeValue);
        typeValue = new[] { x500CertDn };
        _typeRequestCert.InvokeMember("Subject", BindingFlags.PutDispProperty, null, requestCert, typeValue);
        typeValue[0] = x500RootCertDn;
        _typeRequestCert.InvokeMember("Issuer", BindingFlags.PutDispProperty, null, requestCert, typeValue);
        typeValue[0] = validFrom;
        _typeRequestCert.InvokeMember("NotBefore", BindingFlags.PutDispProperty, null, requestCert, typeValue);
        typeValue[0] = validTo;
        _typeRequestCert.InvokeMember("NotAfter", BindingFlags.PutDispProperty, null, requestCert, typeValue);

        var kuExt = Activator.CreateInstance(_typeKuExt);

        typeValue[0] = 176;
        _typeKuExt.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, kuExt, typeValue);

        var certificate =
            _typeRequestCert.InvokeMember("X509Extensions", BindingFlags.GetProperty, null, requestCert, null);
        typeValue = new object[1];

        if (signingCertificate != null)
        {
            typeValue[0] = kuExt;
            _typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, certificate, typeValue);
        }

        typeValue[0] = ekuExt;
        _typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, certificate, typeValue);

        if (signingCertificate != null)
        {
            // add alternative names 
            // https://forums.iis.net/t/1180823.aspx

            var altNameCollection = Activator.CreateInstance(_typeAltNamesCollection);
            var extNames = Activator.CreateInstance(_typeExtNames);
            var altDnsNames = Activator.CreateInstance(_typeCAlternativeName);

            if (IPAddress.TryParse(subject, out IPAddress? ip))
            {
                var ipBase64 = Convert.ToBase64String(ip.GetAddressBytes());
                typeValue = [AlternativeNameType.XcnCertAltNameIpAddress, EncodingType.XcnCryptStringBase64, ipBase64];
                _typeCAlternativeName.InvokeMember("InitializeFromRawData", BindingFlags.InvokeMethod, null, altDnsNames,
                    typeValue);
            }
            else
            {
                typeValue = [3, subject]; //3==DNS, 8==IP ADDR
                _typeCAlternativeName.InvokeMember("InitializeFromString", BindingFlags.InvokeMethod, null, altDnsNames,
                    typeValue);
            }

            typeValue = [altDnsNames];
            _typeAltNamesCollection.InvokeMember("Add", BindingFlags.InvokeMethod, null, altNameCollection,
                typeValue);


            typeValue = [altNameCollection];
            _typeExtNames.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, extNames, typeValue);

            typeValue[0] = extNames;
            _typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, certificate, typeValue);
        }

        if (signingCertificate != null)
        {
            var signerCertificate = Activator.CreateInstance(_typeSignerCertificate);

            typeValue = [0, 0, 12, signingCertificate.Thumbprint];
            _typeSignerCertificate.InvokeMember("Initialize", BindingFlags.InvokeMethod, null, signerCertificate,
                typeValue);
            typeValue = [signerCertificate];
            _typeRequestCert.InvokeMember("SignerCertificate", BindingFlags.PutDispProperty, null, requestCert,
                typeValue);
        }
        else
        {
            var basicConstraints = Activator.CreateInstance(_typeBasicConstraints);

            typeValue = ["true", "0"];
            _typeBasicConstraints.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, basicConstraints,
                typeValue);
            typeValue = [basicConstraints];
            _typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, certificate, typeValue);
        }

        oid = Activator.CreateInstance(_typeOid);

        typeValue = [1, 0, 0, hashAlg];
        _typeOid.InvokeMember("InitializeFromAlgorithmName", BindingFlags.InvokeMethod, null, oid, typeValue);

        typeValue = [oid];
        _typeRequestCert.InvokeMember("HashAlgorithm", BindingFlags.PutDispProperty, null, requestCert, typeValue);
        _typeRequestCert.InvokeMember("Encode", BindingFlags.InvokeMethod, null, requestCert, null);

        var x509Enrollment = Activator.CreateInstance(_typeX509Enrollment);

        typeValue[0] = requestCert;
        _typeX509Enrollment.InvokeMember("InitializeFromRequest", BindingFlags.InvokeMethod, null, x509Enrollment,
            typeValue);

        if (signingCertificate is null)
        {
            typeValue[0] = fullSubject;
            _typeX509Enrollment.InvokeMember("CertificateFriendlyName", BindingFlags.PutDispProperty, null,
                x509Enrollment, typeValue);
        }

        typeValue[0] = 0;

        var createCertRequest = _typeX509Enrollment.InvokeMember("CreateRequest", BindingFlags.InvokeMethod, null,
            x509Enrollment, typeValue);
        typeValue = [2, createCertRequest, 0, string.Empty];

        _typeX509Enrollment.InvokeMember("InstallResponse", BindingFlags.InvokeMethod, null, x509Enrollment,
            typeValue);
        typeValue = [null, 0, 1];

        var empty = _typeX509Enrollment.InvokeMember("CreatePFX", BindingFlags.InvokeMethod, null,
            x509Enrollment, typeValue) as string ?? throw new InvalidOperationException("Could not create PFX");

        return X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(empty), string.Empty, X509KeyStorageFlags.Exportable);
    }
}

public enum EncodingType
{
    XcnCryptStringAny = 7,
    XcnCryptStringBase64 = 1,
    XcnCryptStringBase64Any = 6,
    XcnCryptStringBase64Header = 0,
    XcnCryptStringBase64Requestheader = 3,
    XcnCryptStringBase64Uri = 13,
    XcnCryptStringBase64X509Crlheader = 9,
    XcnCryptStringBinary = 2,
    XcnCryptStringChain = 0x100,
    XcnCryptStringEncodemask = 0xff,
    XcnCryptStringHashdata = 0x10000000,
    XcnCryptStringHex = 4,
    XcnCryptStringHexAny = 8,
    XcnCryptStringHexaddr = 10,
    XcnCryptStringHexascii = 5,
    XcnCryptStringHexasciiaddr = 11,
    XcnCryptStringHexraw = 12,
    XcnCryptStringNocr = -2147483648,
    XcnCryptStringNocrlf = 0x40000000,
    XcnCryptStringPercentescape = 0x8000000,
    XcnCryptStringStrict = 0x20000000,
    XcnCryptStringText = 0x200
}

public enum AlternativeNameType
{
    XcnCertAltNameDirectoryName = 5,
    XcnCertAltNameDnsName = 3,
    XcnCertAltNameGuid = 10,
    XcnCertAltNameIpAddress = 8,
    XcnCertAltNameOtherName = 1,
    XcnCertAltNameRegisteredId = 9,
    XcnCertAltNameRfc822Name = 2,
    XcnCertAltNameUnknown = 0,
    XcnCertAltNameUrl = 7,
    XcnCertAltNameUserPrincipleName = 11
}