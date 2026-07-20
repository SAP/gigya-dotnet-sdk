/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GSAuthMtlsRequest
 * Uses HttpClient with SslStreamCertificateContext for mTLS
 */

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Gigya.Socialize.SDK;

/// <summary>
/// A request class that uses mutual TLS (mTLS) authentication with client certificates.
/// Uses modern HttpClient with SslStreamCertificateContext for proper certificate chain handling.
/// </summary>
public class GSAuthMtlsRequest : GSRequest
{
    /// <summary>
    /// The default domain for mTLS API calls.
    /// </summary>
    public const string DefaultMtlsDomain = "mtls.us1.gigya.com";

    /// <summary>
    /// Cache of HttpClient instances keyed by certificate thumbprint.
    /// This enables connection pooling while supporting multiple certificates.
    /// </summary>
    private static readonly ConcurrentDictionary<string, HttpClient> _httpClientCache = new();

    private readonly MtlsConfig _mtlsConfig;

    /// <summary>
    /// Constructs an mTLS-authenticated request.
    /// </summary>
    /// <param name="apiKey">Gigya's API key from Site Setup.</param>
    /// <param name="apiMethod">The API method to call (e.g., "accounts.getAccountInfo").</param>
    /// <param name="mtlsConfig">The mTLS configuration containing certificate and key.</param>
    /// <param name="clientParams">Optional request parameters.</param>
    /// <param name="additionalHeaders">Optional additional HTTP headers.</param>
    /// <param name="proxy">Optional proxy for HTTP requests.</param>
    public GSAuthMtlsRequest(
        string? apiKey,
        string apiMethod,
        MtlsConfig mtlsConfig,
        object? clientParams = null,
        NameValueCollection? additionalHeaders = null,
        IWebProxy? proxy = null)
        : base(apiKey, null, apiMethod, clientParams, true, null, additionalHeaders, proxy)
    {
        _mtlsConfig = mtlsConfig ?? throw new ArgumentNullException(nameof(mtlsConfig));
        // For mTLS, we don't want to prepend the method namespace to the domain.
        // APIDomain remains as the standard datacenter domain (e.g., "us1.gigya.com");
        // GetRequestDomain dynamically resolves it to the mTLS domain at request time.
        UseMethodDomain = false;
    }

    /// <summary>
    /// Validates the request before sending.
    /// </summary>
    protected override bool IsValidRequest()
    {
        return !string.IsNullOrEmpty(Method);
    }

    /// <summary>
    /// Extracts the datacenter from an API domain and returns the corresponding mTLS domain.
    /// For example, "eu1.gigya.com" returns "mtls.eu1.gigya.com".
    /// Falls back to <see cref="DefaultMtlsDomain"/> when the input is null, empty, or has no dot.
    /// </summary>
    /// <param name="apiDomain">The API domain (e.g., "eu1.gigya.com").</param>
    /// <returns>The mTLS domain for the datacenter, or "mtls.us1.gigya.com" as fallback.</returns>
    public static string GetMtlsDomain(string? apiDomain)
    {
        if (string.IsNullOrWhiteSpace(apiDomain))
            return DefaultMtlsDomain;

        var firstDot = apiDomain.IndexOf('.');
        if (firstDot <= 0)
            return DefaultMtlsDomain;

        var datacenter = apiDomain[..firstDot];
        return $"mtls.{datacenter}.gigya.com";
    }

    /// <summary>
    /// Gets the request domain for mTLS endpoints.
    /// Dynamically resolves the mTLS domain from the configured <see cref="GSRequest.APIDomain"/>.
    /// For example, setting APIDomain to "eu1.gigya.com" routes requests to "mtls.eu1.gigya.com".
    /// </summary>
    protected override string GetRequestDomain(string methodNamespace)
    {
        return GetMtlsDomain(APIDomain);
    }

    /// <summary>
    /// Sets default parameters before signing.
    /// For mTLS, we don't set oauth_token or userKey - just the apiKey in Sign().
    /// </summary>
    protected override void SetDefaultParams(string httpMethod, string resourceUri)
    {
        // mTLS doesn't use the standard auth params - apiKey is set in Sign()
    }

    /// <summary>
    /// Signs the request. For mTLS, this just sets the apiKey parameter.
    /// Authentication is via client certificate, no cryptographic signature needed.
    /// </summary>
    protected override void Sign(string httpMethod, string resourceUri)
    {
        // Api key is required for mTLS requests
        if (ApiKey != null)
        {
            SetParam("apiKey", ApiKey);
        }
    }

    /// <summary>
    /// Gets the HttpClient configured with mTLS client certificate.
    /// Uses a cached HttpClient per certificate thumbprint to enable connection pooling.
    /// </summary>
    protected override HttpClient GetHttpClient()
    {
        var thumbprint = _mtlsConfig.GetCertificateThumbprint();
        return _httpClientCache.GetOrAdd(thumbprint, _ => CreateMtlsHttpClient());
    }

    /// <summary>
    /// Creates an HttpClient configured with the mTLS client certificate.
    /// Uses SslStreamCertificateContext for proper certificate chain handling.
    /// </summary>
    private HttpClient CreateMtlsHttpClient()
    {
        var certPem = _mtlsConfig.LoadCertificate();
        var keyPem = _mtlsConfig.LoadPrivateKey();
        byte[]? pfxBytes = null;

        try
        {
            // Parse private key
            var privateKey = ParsePemPrivateKey(keyPem);
            if (privateKey == null)
            {
                throw new InvalidOperationException("Failed to parse private key from PEM");
            }

            // Parse certificate chain
            var chain = ParseCertificateChain(certPem);
            if (chain.Length == 0)
            {
                throw new InvalidOperationException("No certificates found in PEM");
            }

            // Combine leaf certificate with private key
            var leafCert = chain[0];
            var leafWithKey = leafCert.CopyWithPrivateKey(privateKey);

            // Export to PFX and reload with proper key storage flags.
            // The PFX round-trip is required so the TLS stack can access the private key.
            // We use a randomly generated temporary password (not user-provided) so that
            // no sensitive credential is ever stored as an immutable string in memory.
            var tempPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            pfxBytes = leafWithKey.Export(X509ContentType.Pkcs12, tempPassword);
            var clientCertWithKey = X509CertificateLoader.LoadPkcs12(
                pfxBytes,
                tempPassword,
                X509KeyStorageFlags.Exportable
            );

            // Collect intermediate certificates
            var additionalCerts = new X509Certificate2Collection();
            for (int i = 1; i < chain.Length; i++)
            {
                additionalCerts.Add(chain[i]);
            }

            // Create certificate context with chain for mTLS
            var certContext = SslStreamCertificateContext.Create(clientCertWithKey, additionalCerts);

            // Create handler with mTLS configuration
            // Enable connection pooling for better performance
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                SslOptions = new SslClientAuthenticationOptions
                {
                    ClientCertificateContext = certContext
                    // Server certificate validation uses default system CA store
                }
            };

            // Configure proxy if set
            if (Proxy != null)
            {
                handler.Proxy = Proxy;
                handler.UseProxy = true;
            }

            return new HttpClient(handler)
            {
                Timeout = System.Threading.Timeout.InfiniteTimeSpan
            };
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Failed to load client certificate from PEM", ex);
        }
        finally
        {
            // Clear the PFX bytes from memory
            if (pfxBytes != null)
                Array.Clear(pfxBytes, 0, pfxBytes.Length);
        }
    }

    /// <summary>
    /// Parse certificate chain from PEM format.
    /// </summary>
    private static X509Certificate2[] ParseCertificateChain(string pem)
    {
        var list = new List<X509Certificate2>();
        var regex = new System.Text.RegularExpressions.Regex(
            "-----BEGIN CERTIFICATE-----(.*?)-----END CERTIFICATE-----",
            System.Text.RegularExpressions.RegexOptions.Singleline);
    
        foreach (System.Text.RegularExpressions.Match m in regex.Matches(pem))
        {
            string base64 = m.Groups[1].Value
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(" ", "");
            
            byte[] raw = Convert.FromBase64String(base64);
            list.Add(X509CertificateLoader.LoadCertificate(raw));
        }

        return list.ToArray();
    }

    /// <summary>
    /// Parse RSA private key from PEM format (PKCS#8 or PKCS#1).
    /// </summary>
    private static RSA? ParsePemPrivateKey(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
        {
            return null;
        }

        pem = pem.Trim();
        bool isPkcs8 = pem.Contains("BEGIN PRIVATE KEY", StringComparison.Ordinal);
        bool isPkcs1 = pem.Contains("BEGIN RSA PRIVATE KEY", StringComparison.Ordinal);
        
        // Trim the key headers/footers
        pem = pem
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
            .Replace("-----END RSA PRIVATE KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(" ", "");
            
        byte[] pkcsKey = Convert.FromBase64String(pem);
        RSA rsa = RSA.Create();

        if (!isPkcs8 && !isPkcs1)
        {
            throw new InvalidOperationException("Unsupported private key format. Expected PKCS#8 or PKCS#1 PEM format.");
        }

        if (isPkcs8)
        {
            rsa.ImportPkcs8PrivateKey(pkcsKey, out _);
            return rsa;
        }

        rsa.ImportRSAPrivateKey(pkcsKey, out _);
        return rsa;
    }
}