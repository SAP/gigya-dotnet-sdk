/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - MtlsConfig
 */

using System.Security.Cryptography;
using System.Text;

namespace Gigya.Socialize.SDK;

/// <summary>
/// Configuration holder for mutual TLS (mTLS) settings.
/// Supports both file paths and in-memory PEM content.
/// </summary>
public class MtlsConfig
{
    private readonly string? _certificatePem;
    private readonly string? _certificatePath;
    private readonly string? _privateKeyPem;
    private readonly string? _privateKeyPath;

    private MtlsConfig(
        string? certificatePem,
        string? certificatePath,
        string? privateKeyPem,
        string? privateKeyPath)
    {
        _certificatePem = certificatePem;
        _certificatePath = certificatePath;
        _privateKeyPem = privateKeyPem;
        _privateKeyPath = privateKeyPath;

        Validate();
    }

    #region Factory Methods

    /// <summary>
    /// Creates an MtlsConfig from PEM strings.
    /// </summary>
    /// <param name="certPem">The certificate PEM content.</param>
    /// <param name="keyPem">The private key PEM content.</param>
    /// <returns>A new MtlsConfig instance.</returns>
    public static MtlsConfig FromPem(string certPem, string keyPem)
    {
        return new MtlsConfig(certPem, null, keyPem, null);
    }

    /// <summary>
    /// Creates an MtlsConfig from file paths.
    /// </summary>
    /// <param name="certPath">The path to the certificate PEM file.</param>
    /// <param name="keyPath">The path to the private key PEM file.</param>
    /// <returns>A new MtlsConfig instance.</returns>
    public static MtlsConfig FromFiles(string certPath, string keyPath)
    {
        return new MtlsConfig(null, certPath, null, keyPath);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads the certificate PEM content from memory or file.
    /// </summary>
    /// <returns>The certificate PEM content.</returns>
    /// <exception cref="InvalidOperationException">Thrown if certificate is not available.</exception>
    public string LoadCertificate()
    {
        return LoadFromPemOrFile(_certificatePem, _certificatePath, "Certificate");
    }

    /// <summary>
    /// Loads the private key PEM content from memory or file.
    /// </summary>
    /// <returns>The private key PEM content.</returns>
    /// <exception cref="InvalidOperationException">Thrown if private key is not available.</exception>
    public string LoadPrivateKey()
    {
        return LoadFromPemOrFile(_privateKeyPem, _privateKeyPath, "Private key");
    }

    /// <summary>
    /// Gets the certificate thumbprint (SHA256 hash) for use as a cache key.
    /// </summary>
    /// <returns>The certificate thumbprint as a hex string.</returns>
    public string GetCertificateThumbprint()
    {
        var certPem = LoadCertificate();
        var certBytes = ExtractFirstCertificateBytes(certPem);
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(certBytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Validates that the configuration has all required values.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if configuration is invalid.</exception>
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

    #endregion

    #region Private Methods

    private static string LoadFromPemOrFile(string? pem, string? path, string resourceName)
    {
        if (!string.IsNullOrEmpty(pem))
            return pem;
        
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return File.ReadAllText(path, Encoding.UTF8);
        
        throw new InvalidOperationException($"{resourceName} PEM not provided or file not found");
    }

    private static bool IsValueOrFileProvided(string? pemValue, string? filePath)
    {
        return !string.IsNullOrEmpty(pemValue) ||
               (!string.IsNullOrEmpty(filePath) && File.Exists(filePath));
    }

    private static byte[] ExtractFirstCertificateBytes(string pem)
    {
        const string beginMarker = "-----BEGIN CERTIFICATE-----";
        const string endMarker = "-----END CERTIFICATE-----";

        var startIndex = pem.IndexOf(beginMarker, StringComparison.Ordinal);
        var endIndex = pem.IndexOf(endMarker, StringComparison.Ordinal);

        if (startIndex < 0 || endIndex < 0)
        {
            throw new InvalidOperationException("No certificate found in PEM content");
        }

        var base64 = pem
            .Substring(startIndex + beginMarker.Length, endIndex - startIndex - beginMarker.Length)
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(" ", "");

        return Convert.FromBase64String(base64);
    }

    #endregion
}