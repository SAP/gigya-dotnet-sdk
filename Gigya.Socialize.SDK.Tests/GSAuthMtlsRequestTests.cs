/*
 * Copyright (C) 2024 SAP SE
 * Tests for GSAuthMtlsRequest
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Tests for GSAuthMtlsRequest to verify mTLS-specific behavior.
/// </summary>
public class GSAuthMtlsRequestTests
{
    #region DefaultMtlsDomain Constant Tests

    [Fact]
    public void DefaultMtlsDomain_HasCorrectValue()
    {
        Assert.Equal("mtls.us1.gigya.com", GSAuthMtlsRequest.DefaultMtlsDomain);
    }

    #endregion

    #region GetMtlsDomain Tests

    [Theory]
    [InlineData("us1.gigya.com", "mtls.us1.gigya.com")]
    [InlineData("eu1.gigya.com", "mtls.eu1.gigya.com")]
    [InlineData("eu2.gigya.com", "mtls.eu2.gigya.com")]
    [InlineData("au1.gigya.com", "mtls.au1.gigya.com")]
    [InlineData("global.gigya.com", "mtls.global.gigya.com")]
    [InlineData("us1-st1.gigya.com", "mtls.us1-st1.gigya.com")]
    [InlineData("cn1.sapcdm.cn", "mtls.cn1.gigya.com")]
    [InlineData("il1-cdp.gigya.com", "mtls.il1-cdp.gigya.com")]
    public void GetMtlsDomain_ExtractsDatacenterCorrectly(string apiDomain, string expectedMtlsDomain)
    {
        // Act
        var result = GSAuthMtlsRequest.GetMtlsDomain(apiDomain);

        // Assert
        Assert.Equal(expectedMtlsDomain, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetMtlsDomain_WithNullOrEmptyInput_ReturnsDefaultMtlsDomain(string? apiDomain)
    {
        // Act
        var result = GSAuthMtlsRequest.GetMtlsDomain(apiDomain);

        // Assert
        Assert.Equal(GSAuthMtlsRequest.DefaultMtlsDomain, result);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("nodot")]
    public void GetMtlsDomain_WithNoDot_ReturnsDefaultMtlsDomain(string apiDomain)
    {
        // Act
        var result = GSAuthMtlsRequest.GetMtlsDomain(apiDomain);

        // Assert
        Assert.Equal(GSAuthMtlsRequest.DefaultMtlsDomain, result);
    }

    [Fact]
    public void GetMtlsDomain_WithDotAtStart_ReturnsDefaultMtlsDomain()
    {
        // Act
        var result = GSAuthMtlsRequest.GetMtlsDomain(".gigya.com");

        // Assert
        Assert.Equal(GSAuthMtlsRequest.DefaultMtlsDomain, result);
    }

    #endregion

    #region APIDomain Property Tests

    [Fact]
    public void APIDomain_ReturnsBaseClassDefault()
    {
        // Arrange
        var mtlsConfig = CreateTestMtlsConfig();
        var request = new GSAuthMtlsRequest("test-api-key", "accounts.getAccountInfo", mtlsConfig);

        // Act
        var domain = request.APIDomain;

        // Assert - APIDomain inherits from base class default "us1.gigya.com"
        // GetRequestDomain will convert this to "mtls.us1.gigya.com" at request time
        Assert.Equal("us1.gigya.com", domain);
    }

    [Fact]
    public void APIDomain_CanBeOverwritten()
    {
        // Arrange
        var mtlsConfig = CreateTestMtlsConfig();
        var request = new GSAuthMtlsRequest("test-api-key", "accounts.getAccountInfo", mtlsConfig);

        // Act - set a different datacenter domain
        request.APIDomain = "eu1.gigya.com";

        // Assert - should return the custom domain
        Assert.Equal("eu1.gigya.com", request.APIDomain);
    }

    [Fact]
    public void APIDomain_CanBeSetMultipleTimes()
    {
        // Arrange
        var mtlsConfig = CreateTestMtlsConfig();
        var request = new GSAuthMtlsRequest("test-api-key", "accounts.getAccountInfo", mtlsConfig);

        // Act - set different domains multiple times
        request.APIDomain = "us1.gigya.com";
        Assert.Equal("us1.gigya.com", request.APIDomain);

        request.APIDomain = "eu1.gigya.com";
        Assert.Equal("eu1.gigya.com", request.APIDomain);

        request.APIDomain = "au1.gigya.com";
        Assert.Equal("au1.gigya.com", request.APIDomain);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParams_CreatesRequest()
    {
        // Arrange
        var mtlsConfig = CreateTestMtlsConfig();

        // Act
        var request = new GSAuthMtlsRequest("test-api-key", "accounts.getAccountInfo", mtlsConfig);

        // Assert
        Assert.NotNull(request);
    }

    [Fact]
    public void Constructor_WithNullMtlsConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GSAuthMtlsRequest("test-api-key", "accounts.getAccountInfo", null!));
    }

    [Fact]
    public void Constructor_SetsUseMethodDomainToFalse()
    {
        // Arrange
        var mtlsConfig = CreateTestMtlsConfig();

        // Act
        var request = new GSAuthMtlsRequest("test-api-key", "accounts.getAccountInfo", mtlsConfig);

        // Assert
        Assert.False(request.UseMethodDomain);
    }

    [Fact]
    public void Constructor_InheritsDefaultAPIDomainFromBaseClass()
    {
        // Arrange
        var mtlsConfig = CreateTestMtlsConfig();

        // Act
        var request = new GSAuthMtlsRequest("test-api-key", "accounts.getAccountInfo", mtlsConfig);

        // Assert - APIDomain should be the base class default "us1.gigya.com"
        // The mTLS domain resolution happens in GetRequestDomain, not in the constructor
        Assert.Equal("us1.gigya.com", request.APIDomain);
    }

    #endregion

    #region Parameter Tests

    [Fact]
    public void SetParam_String_SetsParameter()
    {
        // Arrange
        var mtlsConfig = CreateTestMtlsConfig();
        var request = new GSAuthMtlsRequest("test-api-key", "accounts.getAccountInfo", mtlsConfig);

        // Act
        request.SetParam("UID", "test-uid");

        // Assert
        var parameters = request.GetParams();
        Assert.Equal("test-uid", parameters.GetString("UID", null));
    }

    [Fact]
    public void Constructor_WithClientParams_SetsParameters()
    {
        // Arrange
        var mtlsConfig = CreateTestMtlsConfig();
        var clientParams = new GSObject();
        clientParams.Put("UID", "test-uid");

        // Act
        var request = new GSAuthMtlsRequest("test-api-key", "accounts.getAccountInfo", mtlsConfig, clientParams);

        // Assert
        var parameters = request.GetParams();
        Assert.Equal("test-uid", parameters.GetString("UID", null));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test MtlsConfig with dummy certificate and key data.
    /// Note: This config is for testing constructor behavior only, not for actual mTLS connections.
    /// </summary>
    private static MtlsConfig CreateTestMtlsConfig()
    {
        // Create a minimal MtlsConfig for testing
        // The certificate and key are not valid, but sufficient for constructor tests
        return MtlsConfig.FromPem(
            "-----BEGIN CERTIFICATE-----\nMIIBkTCB+wIJAKHBfpegPjMCMA0GCSqGSIb3DQEBCwUAMBExDzANBgNVBAMMBnRl\nc3RjYTAeFw0yNDAxMDEwMDAwMDBaFw0yNTAxMDEwMDAwMDBaMBExDzANBgNVBAMM\nBnRlc3RjYTBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQC7o96FCFzJLssfMKLCPPEB\nxTCLwBPOwVYzOtA4K+5ynJPfNq2AbwIDAQABo1MwUTAdBgNVHQ4EFgQUtest0000\n0000000000000000000wHwYDVR0jBBgwFoAUtest00000000000000000000000wDw\nYDVR0TAQH/BAUwAwEB/zANBgkqhkiG9w0BAQsFAANBAA==\n-----END CERTIFICATE-----",
            "-----BEGIN PRIVATE KEY-----\nMIIBVQIBADANBgkqhkiG9w0BAQEFAASCAT8wggE7AgEAAkEAu6PehQhcyS7LHzCi\nwjzxAcUwi8ATzsFWMzrQOCvucpyT3zatgG8CAQACQQCtest0000000000000000\n-----END PRIVATE KEY-----"
        );
    }

    #endregion
}