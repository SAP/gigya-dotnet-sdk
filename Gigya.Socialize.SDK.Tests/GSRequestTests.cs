/*
 * Copyright (C) 2024 SAP SE
 * Characterization tests for GSRequest
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Characterization tests for GSRequest to verify behavior matches legacy SDK.
/// </summary>
public class GSRequestTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithAccessToken_CreatesRequest()
    {
        var request = new GSRequest("test-access-token", "accounts.getAccountInfo");
        
        Assert.NotNull(request);
    }

    [Fact]
    public void Constructor_WithAccessTokenAndParams_CreatesRequest()
    {
        var gsParams = new GSObject();
        gsParams.Put("UID", "test-uid");
        var request = new GSRequest("test-access-token", "accounts.getAccountInfo", gsParams);
        
        var parameters = request.GetParams();
        Assert.Equal("test-uid", parameters.GetString("UID", null));
    }

    [Fact]
    public void Constructor_WithApiKeyAndSecret_CreatesRequest()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        Assert.NotNull(request);
    }

    [Fact]
    public void Constructor_WithApiKeySecretAndHttps_CreatesRequest()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo", true);
        
        Assert.NotNull(request);
    }

    [Fact]
    public void Constructor_WithApiKeySecretAndParams_CreatesRequest()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo", new { UID = "test-uid" });
        
        var parameters = request.GetParams();
        Assert.Equal("test-uid", parameters.GetString("UID", null));
    }

    [Fact]
    public void Constructor_WithGSObjectParams_ClonesParams()
    {
        var gsParams = new GSObject();
        gsParams.Put("UID", "test-uid");
        
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo", gsParams);
        
        // Modify original - should not affect request
        gsParams.Put("UID", "modified");
        
        var parameters = request.GetParams();
        Assert.Equal("test-uid", parameters.GetString("UID", null));
    }

    #endregion

    #region SetParam Tests

    [Fact]
    public void SetParam_String_SetsParameter()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        request.SetParam("UID", "test-uid");
        
        var parameters = request.GetParams();
        Assert.Equal("test-uid", parameters.GetString("UID", null));
    }

    [Fact]
    public void SetParam_Int_SetsParameter()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        request.SetParam("count", 42);
        
        var parameters = request.GetParams();
        Assert.Equal(42, parameters.GetInt("count", 0));
    }

    [Fact]
    public void SetParam_Long_SetsParameter()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        request.SetParam("timestamp", 9876543210L);
        
        var parameters = request.GetParams();
        Assert.Equal(9876543210L, parameters.GetLong("timestamp", 0));
    }

    [Fact]
    public void SetParam_Bool_SetsParameter()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        request.SetParam("active", true);
        
        var parameters = request.GetParams();
        Assert.True(parameters.GetBool("active", false));
    }

    [Fact]
    public void SetParam_GSObject_SetsParameter()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        var nested = new GSObject();
        nested.Put("inner", "value");
        
        request.SetParam("profile", nested);
        
        var parameters = request.GetParams();
        var retrieved = parameters.GetObject("profile", null);
        Assert.NotNull(retrieved);
        Assert.Equal("value", retrieved!.GetString("inner", null));
    }

    [Fact]
    public void SetParam_GSArray_SetsParameter()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        var arr = new GSArray();
        arr.Add("item1");
        arr.Add("item2");
        
        request.SetParam("items", arr);
        
        var parameters = request.GetParams();
        var retrieved = parameters.GetArray("items", null);
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved!.Length);
    }

    #endregion

    #region GetParams Tests

    [Fact]
    public void GetParams_ReturnsAllParameters()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        request.SetParam("param1", "value1");
        request.SetParam("param2", 42);
        
        var parameters = request.GetParams();
        
        Assert.Equal("value1", parameters.GetString("param1", null));
        Assert.Equal(42, parameters.GetInt("param2", 0));
    }

    #endregion

    #region Static Configuration Tests

    [Fact]
    public void Version_ReturnsVersionString()
    {
        Assert.Equal("3.0.0", GSRequest.Version);
    }

    [Fact]
    public void EnableConnectionPooling_DefaultIsTrue()
    {
        Assert.True(GSRequest.EnableConnectionPooling);
    }

    [Fact]
    public void MaxConcurrentConnections_DefaultIs100()
    {
        Assert.Equal(100, GSRequest.MaxConcurrentConnections);
    }

    // BlockWhenConnectionsExhausted was removed in the modern SDK
    // Connection pooling is handled by SocketsHttpHandler automatically

    [Fact]
    public void MaxResponseSize_DefaultIs50MB()
    {
        Assert.Equal(50u * 1024 * 1024, GSRequest.MaxResponseSize);
    }

    #endregion

    #region Instance Configuration Tests

    [Fact]
    public void APIDomain_DefaultIsUs1()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        Assert.Equal("us1.gigya.com", request.APIDomain);
    }

    [Fact]
    public void APIDomain_CanBeChanged()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        request.APIDomain = "eu1.gigya.com";
        
        Assert.Equal("eu1.gigya.com", request.APIDomain);
    }

    [Fact]
    public void UseMethodDomain_DefaultIsTrue()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        Assert.True(request.UseMethodDomain);
    }

    [Fact]
    public void RecoverFromExpiredConnections_DefaultIsTrue()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        Assert.True(request.RecoverFromExpiredConnections);
    }

    [Fact]
    public void Proxy_DefaultIsNull()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
        Assert.Null(request.Proxy);
    }

    #endregion

    #region BuildQS Tests

    [Fact]
    public void BuildQS_WithQuestionMark_AddsPrefix()
    {
        var parameters = new GSObject();
        parameters.Put("key", "value");
        
        var qs = GSRequest.BuildQS(true, parameters);
        
        Assert.StartsWith("?", qs);
    }

    [Fact]
    public void BuildQS_WithoutQuestionMark_NoPrefix()
    {
        var parameters = new GSObject();
        parameters.Put("key", "value");
        
        var qs = GSRequest.BuildQS(false, parameters);
        
        Assert.DoesNotContain("?", qs);
    }

    [Fact]
    public void BuildQS_MultipleParams_JoinsWithAmpersand()
    {
        var parameters = new GSObject();
        parameters.Put("key1", "value1");
        parameters.Put("key2", "value2");
        
        var qs = GSRequest.BuildQS(false, parameters);
        
        Assert.Contains("key1=value1", qs);
        Assert.Contains("key2=value2", qs);
        Assert.Contains("&", qs);
    }

    [Fact]
    public void BuildQS_EmptyParams_ReturnsEmpty()
    {
        var parameters = new GSObject();
        
        var qs = GSRequest.BuildQS(false, parameters);
        
        Assert.Empty(qs);
    }

    #endregion

    #region UrlEncode Tests

    [Fact]
    public void UrlEncode_AlphanumericChars_NotEncoded()
    {
        var result = GSRequest.UrlEncode("abc123");
        
        Assert.Equal("abc123", result);
    }

    [Fact]
    public void UrlEncode_UnreservedChars_NotEncoded()
    {
        var result = GSRequest.UrlEncode("-_.~");
        
        Assert.Equal("-_.~", result);
    }

    [Fact]
    public void UrlEncode_Space_EncodedAsPercent20()
    {
        var result = GSRequest.UrlEncode("hello world");
        
        Assert.Equal("hello%20world", result);
    }

    [Fact]
    public void UrlEncode_SpecialChars_Encoded()
    {
        var result = GSRequest.UrlEncode("a+b=c&d");
        
        Assert.Equal("a%2Bb%3Dc%26d", result);
    }

    [Fact]
    public void UrlEncode_UnicodeChars_EncodedAsUtf8()
    {
        var result = GSRequest.UrlEncode("日本語");
        
        // UTF-8 encoding of Japanese characters
        Assert.Contains("%", result);
        Assert.DoesNotContain("日", result);
    }

    [Fact]
    public void UrlEncode_EmptyString_ReturnsEmpty()
    {
        var result = GSRequest.UrlEncode("");
        
        Assert.Empty(result);
    }

    #endregion

    #region Send Validation Tests

    [Fact]
    public void Send_EmptyMethod_ReturnsError()
    {
        var request = new GSRequest("test-api-key", "test-secret", "");
        
        var response = request.Send();
        
        Assert.NotEqual(0, response.GetErrorCode());
    }

    [Fact]
    public void Send_NullApiKey_ReturnsError()
    {
        var request = new GSRequest(null, "test-secret", "accounts.getAccountInfo", null, false);
        
        var response = request.Send();
        
        Assert.NotEqual(0, response.GetErrorCode());
    }

    #endregion

    #region Legacy Async Methods Tests

    [Fact]
    public void BeginSend_ReturnsAsyncResult()
    {
        var request = new GSRequest("test-api-key", "test-secret", "");
        
#pragma warning disable CS0618 // Type or member is obsolete
        var asyncResult = request.BeginSend(null, null);
#pragma warning restore CS0618
        
        Assert.NotNull(asyncResult);
    }

    [Fact]
    public void Abort_DoesNotThrow()
    {
        var request = new GSRequest("test-api-key", "test-secret", "accounts.getAccountInfo");
        
#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Record.Exception(() => request.Abort());
#pragma warning restore CS0618
        
        Assert.Null(exception);
    }

    #endregion
}