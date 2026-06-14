using System;
using System.IO;
using System.Text;

namespace Gigya.Socialize.SDK
{
    /// <summary>
    /// MtlsConfig - Configuration holder for mutual TLS (mTLS) settings.
    /// Supports both file paths and in-memory PEM content.
    /// </summary>
    public class MtlsConfig
    {
        private readonly string _certificatePem;
        private readonly string _certificatePath;

        private readonly string _privateKeyPem;
        private readonly string _privateKeyPath;

        private readonly char[] _keyStorePassword;

        private MtlsConfig(
            string certificatePem,
            string certificatePath,
            string privateKeyPem,
            string privateKeyPath,
            char[] keyStorePassword)
        {
            this._certificatePem = certificatePem;
            this._certificatePath = certificatePath;
            this._privateKeyPem = privateKeyPem;
            this._privateKeyPath = privateKeyPath;
            this._keyStorePassword = (keyStorePassword != null)
                ? (char[])keyStorePassword.Clone()
                : "changeit".ToCharArray();

            Validate();
        }

        // ---------- STATIC FACTORY METHODS ---------- //

        /// <summary>
        /// Use PEM strings, default password
        /// </summary>
        public static MtlsConfig FromPem(string certPem, string keyPem)
        {
            return new MtlsConfig(certPem, null, keyPem, null, null);
        }

        /// <summary>
        /// Use PEM strings + custom password
        /// </summary>
        public static MtlsConfig FromPem(string certPem, string keyPem, char[] password)
        {
            return new MtlsConfig(certPem, null, keyPem, null, password);
        }

        /// <summary>
        /// Use file paths, default password
        /// </summary>
        public static MtlsConfig FromFiles(string certPath, string keyPath)
        {
            return new MtlsConfig(null, certPath, null, keyPath, null);
        }

        /// <summary>
        /// Use file paths + custom password
        /// </summary>
        public static MtlsConfig FromFiles(string certPath, string keyPath, char[] password)
        {
            return new MtlsConfig(null, certPath, null, keyPath, password);
        }

        public char[] GetPassword()
        {
            return _keyStorePassword != null ? (char[])_keyStorePassword.Clone() : "changeit".ToCharArray();
        }

        public string LoadCertificate()
        {
            return LoadFromPemOrFile(_certificatePem, _certificatePath, "Certificate");
        }

        public string LoadPrivateKey()
        {
            return LoadFromPemOrFile(_privateKeyPem, _privateKeyPath, "Private key");
        }

        private string LoadFromPemOrFile(string pem, string path, string resourceName)
        {
            if (pem != null && pem.Length > 0) return pem;
            if (path != null && File.Exists(path))
                return File.ReadAllText(path, Encoding.UTF8);
            throw new InvalidOperationException($"{resourceName} PEM not provided or file not found");
        }

        public void Validate()
        {
            if (!IsValueOrFileProvided(_certificatePem, _certificatePath))
            {
                throw new InvalidOperationException("mTLS certificate missing (no PEM or file path provided)");
            }

            if (!IsValueOrFileProvided(_privateKeyPem, _privateKeyPath))
            {
                throw new InvalidOperationException("mTLS private key missing (no PEM or file path provided)");
            }
        }

        private bool IsValueOrFileProvided(string pemValue, string filePath)
        {
            return (pemValue != null && pemValue.Length > 0) ||
                   (filePath != null && File.Exists(filePath));
        }
    }
}
