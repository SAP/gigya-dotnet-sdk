using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Gigya.Socialize.SDK
{
    /// <summary>
    /// GSAuthMtlsRequest - A request class that uses mutual TLS (mTLS) authentication.
    /// Accepts client certificate & private key via PEM string or file path (either form is acceptable).
    /// </summary>
    public class GSAuthMtlsRequest : GSRequest
    {
        private readonly MtlsConfig mtlsConfig;
        
        /// <param name="apiKey">Site API key.</param>
        /// <param name="apiMethod">The API method to call (e.g., "accounts.getAccountInfo")</param>
        /// <param name="mtlsConfig">The mTLS configuration containing certificate and private key (as PEM strings or file paths)</param>
        public GSAuthMtlsRequest(string apiKey, string apiMethod, MtlsConfig mtlsConfig)
            : base(apiKey, null, apiMethod, null, true)
        {
            if (mtlsConfig == null)
            {
                throw new ArgumentNullException(nameof(mtlsConfig), "MtlsConfig cannot be null");
            }
            this.mtlsConfig = mtlsConfig;
            mtlsConfig.Validate();
        }
        
        /// <summary>
        /// mTLS uses the client certificate as the credential, so don't send oauth_token.
        /// Only the apiKey is required so the server can identify the site.
        /// </summary>
        protected override void SetDefaultParams(string httpMethod, string resourceUri)
        {
            if (ApiKey != null)
            {
                SetParam("apiKey", ApiKey);
            }
        }

        protected override void Sign(string httpMethod, string resourceUri)
        {
            // mTLS does not require request signing; the client certificate is the credential.
        }
        
        protected override bool IsValidRequest()
        {
            return true;
        }

        /// <summary>
        /// Resolves the mTLS host based on the configured API domain.
        /// Extracts the datacenter (the first segment before the first dot) from
        /// <see cref="GSRequest.APIDomain"/> and returns "mtls.{datacenter}.gigya.com".
        /// Falls back to "mtls.us1.gigya.com" when the domain is unset or has no datacenter segment.
        /// </summary>
        public string GetMtlsDomain()
        {
            string apiDomain = APIDomain ?? string.Empty;
            int dotIndex = apiDomain.IndexOf('.');
            string datacenter = dotIndex > 0 ? apiDomain.Substring(0, dotIndex) : apiDomain;
            if (string.IsNullOrEmpty(datacenter))
            {
                return "mtls.us1.gigya.com";
            }
            return "mtls." + datacenter + ".gigya.com";
        }

        /// <summary>
        /// Route mTLS requests to the datacenter-specific endpoint
        /// (e.g. mtls.eu1.gigya.com for an APIDomain of eu1.gigya.com).
        /// </summary>
        protected override string GetRequestDomain(string methodNamespace)
        {
            return GetMtlsDomain();
        }

        /// <summary>
        /// Override hook method to apply client certificates to HTTPS connections.
        /// This method is called by GSRequest.Send() after creating the connection.
        /// </summary>
        protected override void ConfigureRequest(HttpWebRequest request)
        {
            if (request == null || !request.RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            CertificateBundle? bundle = LoadCertificates();
            if (bundle == null)
            {
                throw new InvalidOperationException("Failed to load client certificates - cannot proceed with mTLS");
            }

            try
            {
                X509Certificate2 clientCertificate = CreateClientCertificate(bundle);
                ApplyCertificatesToConnection(request, clientCertificate);
            }
            catch (Exception e)
            {
                Logger.Write("GSAuthMtlsRequest", "Failed to configure mTLS: " + e.Message);
                Logger.Write(e);
                throw new InvalidOperationException("Failed to configure mTLS: " + e.Message, e);
            }
        }

        /// <summary>
        /// Load certificates from mTLS configuration.
        /// </summary>
        private CertificateBundle? LoadCertificates()
        {
            try
            {
                string certPem = mtlsConfig.LoadCertificate();
                string keyPem = mtlsConfig.LoadPrivateKey();
                RSA? privateKey = ParsePemPrivateKey(keyPem);
                X509Certificate2[] chain = ParseCertificateChain(certPem);

                if (privateKey == null || chain.Length == 0)
                {
                    return null;
                }

                return new CertificateBundle(privateKey, chain);
            }
            catch (Exception e)
            {
                Logger.Write("GSAuthMtlsRequest", "Error loading certificates: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Create a client certificate with private key from the certificate bundle.
        /// </summary>
        private X509Certificate2 CreateClientCertificate(CertificateBundle bundle)
        {
            try
            {
                char[] password = mtlsConfig.GetPassword();
                string pfxPassword = new string(password);

                // Build keystore (PFX) with private key and certificate chain
                var keyStore = new X509Certificate2Collection();
                
                var leafCert = bundle.Chain[0];
                var intermediateCert = bundle.Chain[1];
                
                // Combine leaf certificate with private key
                var leafWithKey = leafCert.CopyWithPrivateKey(bundle.PrivateKey);
                keyStore.Add(leafWithKey);
                keyStore.Add(intermediateCert);

                // Export to PFX bytes
                byte[]? pfxBytes = keyStore.Export(X509ContentType.Pkcs12, pfxPassword);

                var finalCert = X509CertificateLoader.LoadPkcs12(
                    pfxBytes,
                    pfxPassword,
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable
                );

                if (!finalCert.HasPrivateKey)
                {
                    throw new InvalidOperationException("Final certificate does not contain private key after PFX import");
                }
                
                // Clean up sensitive data
                Array.Clear(pfxBytes, 0, pfxBytes.Length);
                Array.Clear(password, 0, password.Length);

                return finalCert;
            }
            catch (Exception e)
            {
                Logger.Write("GSAuthMtlsRequest", "Error creating client certificate: " + e.Message);
                throw;
            }
        }

        /// <summary>
        /// Apply the client certificate to the HTTPS connection.
        /// </summary>
        private void ApplyCertificatesToConnection(HttpWebRequest request, X509Certificate2 clientCertificate)
        {
            request.ClientCertificates.Add(clientCertificate);
        }
        
        /// <summary>
        /// Inner class to hold certificate bundle (private key + certificate chain).
        /// </summary>
        private class CertificateBundle
        {
            public RSA PrivateKey { get; }
            public X509Certificate2[] Chain { get; }

            public CertificateBundle(RSA privateKey, X509Certificate2[] chain)
            {
                PrivateKey = privateKey;
                Chain = chain;
            }
        }
        
        // ----------------- Certificate parsing helpers -----------------
        
        /// <summary>
        /// Parse certificate chain from PEM format.
        /// </summary>
        private static X509Certificate2[] ParseCertificateChain(string pem)
        {
            var list = new List<X509Certificate2>();
            var regex = new Regex("-----BEGIN CERTIFICATE-----(.*?)-----END CERTIFICATE-----",
                RegexOptions.Singleline);
        
            foreach (Match m in regex.Matches(pem))
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
            pem = TrimKey(pem);
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
        
        private static String TrimKey(String pem) {
            return pem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("\\s+", "");
        }
        
    }
}

