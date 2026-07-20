/*
 * Copyright (C) 2024 SAP SE
 * Characterization tests for MtlsConfig
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Characterization tests for MtlsConfig to verify behavior matches legacy SDK.
/// </summary>
public class MtlsConfigTests
{
    // Sample PEM content for testing (not real certificates)
    private const string SampleCertPem = @"-----BEGIN CERTIFICATE-----
MIIBkTCB+wIJAKHBfpegPjMCMA0GCSqGSIb3DQEBCwUAMBExDzANBgNVBAMMBnRl
c3RjYTAeFw0yNDAxMDEwMDAwMDBaFw0yNTAxMDEwMDAwMDBaMBExDzANBgNVBAMM
BnRlc3RjYTBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQC7o96FCFzJLssfMKLCPPEh
xo8MJKkKO5sVvtgBg7fWe8JXqZpFqm1qanpFMwMKvua8hG1LRryMN1qPKP8AgSfV
AgMBAAGjUzBRMB0GA1UdDgQWBBQExample0000000000000000000MB8GA1UdIwQY
MBaAFBQExample0000000000000000000MA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZI
hvcNAQELBQADQQBExample000000000000000000000000000000000000000000
-----END CERTIFICATE-----";

    private const string SampleKeyPem = @"-----BEGIN PRIVATE KEY-----
MIIBVQIBADANBgkqhkiG9w0BAQEFAASCAT8wggE7AgEAAkEAu6PehQhcyS7LHzCi
wjzxIcaPDCSpCjubFb7YAYO31nvCV6maRaptamp6RTMDCr7mvIRtS0a8jDdajyj/
AIEn1QIDAQABAkBExample00000000000000000000000000000000000000000
-----END PRIVATE KEY-----";

    #region FromPem Tests

    [Fact]
    public void FromPem_WithValidPems_CreatesConfig()
    {
        var config = MtlsConfig.FromPem(SampleCertPem, SampleKeyPem);
        
        Assert.NotNull(config);
    }

    [Fact]
    public void FromPem_WithEmptyCert_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => 
            MtlsConfig.FromPem("", SampleKeyPem));
    }

    [Fact]
    public void FromPem_WithEmptyKey_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => 
            MtlsConfig.FromPem(SampleCertPem, ""));
    }

    [Fact]
    public void FromPem_WithNullCert_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => 
            MtlsConfig.FromPem(null!, SampleKeyPem));
    }

    [Fact]
    public void FromPem_WithNullKey_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => 
            MtlsConfig.FromPem(SampleCertPem, null!));
    }

    #endregion

    #region FromFiles Tests

    [Fact]
    public void FromFiles_WithNonExistentCertFile_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => 
            MtlsConfig.FromFiles("nonexistent-cert.pem", "nonexistent-key.pem"));
    }

    [Fact]
    public void FromFiles_WithEmptyPaths_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => 
            MtlsConfig.FromFiles("", ""));
    }

    #endregion

    #region LoadCertificate Tests

    [Fact]
    public void LoadCertificate_FromPem_ReturnsCertContent()
    {
        var config = MtlsConfig.FromPem(SampleCertPem, SampleKeyPem);
        
        var cert = config.LoadCertificate();
        
        Assert.Equal(SampleCertPem, cert);
    }

    [Fact]
    public void LoadCertificate_ContainsCertificateMarkers()
    {
        var config = MtlsConfig.FromPem(SampleCertPem, SampleKeyPem);
        
        var cert = config.LoadCertificate();
        
        Assert.Contains("BEGIN CERTIFICATE", cert);
        Assert.Contains("END CERTIFICATE", cert);
    }

    #endregion

    #region LoadPrivateKey Tests

    [Fact]
    public void LoadPrivateKey_FromPem_ReturnsKeyContent()
    {
        var config = MtlsConfig.FromPem(SampleCertPem, SampleKeyPem);
        
        var key = config.LoadPrivateKey();
        
        Assert.Equal(SampleKeyPem, key);
    }

    [Fact]
    public void LoadPrivateKey_ContainsPrivateKeyMarkers()
    {
        var config = MtlsConfig.FromPem(SampleCertPem, SampleKeyPem);
        
        var key = config.LoadPrivateKey();
        
        Assert.Contains("BEGIN PRIVATE KEY", key);
        Assert.Contains("END PRIVATE KEY", key);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_ValidConfig_DoesNotThrow()
    {
        var config = MtlsConfig.FromPem(SampleCertPem, SampleKeyPem);
        
        var exception = Record.Exception(() => config.Validate());
        
        Assert.Null(exception);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void MtlsConfig_CanBeUsedMultipleTimes()
    {
        var config = MtlsConfig.FromPem(SampleCertPem, SampleKeyPem);
        
        // Load multiple times
        var cert1 = config.LoadCertificate();
        var cert2 = config.LoadCertificate();
        var key1 = config.LoadPrivateKey();
        var key2 = config.LoadPrivateKey();
        
        Assert.Equal(cert1, cert2);
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void MtlsConfig_PreservesWhitespaceInPem()
    {
        var config = MtlsConfig.FromPem(SampleCertPem, SampleKeyPem);
        
        var cert = config.LoadCertificate();
        
        // PEM format requires specific line breaks
        Assert.Contains("\n", cert);
    }

    #endregion
}