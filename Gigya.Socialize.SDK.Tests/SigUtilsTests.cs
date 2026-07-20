/*
 * Copyright (C) 2024 SAP SE
 * Characterization tests for SigUtils
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Characterization tests for SigUtils to verify behavior matches legacy SDK.
/// </summary>
public class SigUtilsTests
{
    // Test secret key (Base64 encoded) - for testing only
    private const string TestSecret = "dGVzdHNlY3JldGtleWZvcnRlc3Rpbmc="; // "testsecretkeyfortesting" in Base64

    #region CalcSignature Tests

    [Fact]
    public void CalcSignature_SameInput_ProducesSameOutput()
    {
        var text = "test_message";
        
        var sig1 = SigUtils.CalcSignature(text, TestSecret);
        var sig2 = SigUtils.CalcSignature(text, TestSecret);
        
        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void CalcSignature_DifferentInput_ProducesDifferentOutput()
    {
        var sig1 = SigUtils.CalcSignature("message1", TestSecret);
        var sig2 = SigUtils.CalcSignature("message2", TestSecret);
        
        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void CalcSignature_DifferentKey_ProducesDifferentOutput()
    {
        var text = "test_message";
        var otherSecret = "b3RoZXJzZWNyZXRrZXlmb3J0ZXN0aW5n"; // "othersecretkeyfortesting" in Base64
        
        var sig1 = SigUtils.CalcSignature(text, TestSecret);
        var sig2 = SigUtils.CalcSignature(text, otherSecret);
        
        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void CalcSignature_ReturnsBase64String()
    {
        var sig = SigUtils.CalcSignature("test", TestSecret);
        
        // Should be valid Base64
        var bytes = Convert.FromBase64String(sig);
        Assert.NotEmpty(bytes);
    }

    #endregion

    #region ValidateUserSignature Tests

    [Fact]
    public void ValidateUserSignature_ValidSignature_ReturnsTrue()
    {
        var uid = "user123";
        var timestamp = "1234567890";
        var expectedSig = SigUtils.CalcSignature(timestamp + "_" + uid, TestSecret);
        
        var result = SigUtils.ValidateUserSignature(uid, timestamp, TestSecret, expectedSig);
        
        Assert.True(result);
    }

    [Fact]
    public void ValidateUserSignature_InvalidSignature_ReturnsFalse()
    {
        var uid = "user123";
        var timestamp = "1234567890";
        var invalidSig = "invalid_signature";
        
        var result = SigUtils.ValidateUserSignature(uid, timestamp, TestSecret, invalidSig);
        
        Assert.False(result);
    }

    [Fact]
    public void ValidateUserSignature_WrongUid_ReturnsFalse()
    {
        var uid = "user123";
        var timestamp = "1234567890";
        var sig = SigUtils.CalcSignature(timestamp + "_" + uid, TestSecret);
        
        var result = SigUtils.ValidateUserSignature("wronguser", timestamp, TestSecret, sig);
        
        Assert.False(result);
    }

    [Fact]
    public void ValidateUserSignature_WrongTimestamp_ReturnsFalse()
    {
        var uid = "user123";
        var timestamp = "1234567890";
        var sig = SigUtils.CalcSignature(timestamp + "_" + uid, TestSecret);
        
        var result = SigUtils.ValidateUserSignature(uid, "9999999999", TestSecret, sig);
        
        Assert.False(result);
    }

    [Fact]
    public void ValidateUserSignature_WithExpiration_ValidNotExpired_ReturnsTrue()
    {
        var uid = "user123";
        var currentTime = SigUtils.CurrentTimeSeconds();
        var timestamp = currentTime.ToString();
        var sig = SigUtils.CalcSignature(timestamp + "_" + uid, TestSecret);
        
        var result = SigUtils.ValidateUserSignature(uid, timestamp, TestSecret, sig, 300);
        
        Assert.True(result);
    }

    [Fact]
    public void ValidateUserSignature_WithExpiration_Expired_ReturnsFalse()
    {
        var uid = "user123";
        var oldTimestamp = "1000000000"; // Very old timestamp
        var sig = SigUtils.CalcSignature(oldTimestamp + "_" + uid, TestSecret);
        
        var result = SigUtils.ValidateUserSignature(uid, oldTimestamp, TestSecret, sig, 300);
        
        Assert.False(result);
    }

    #endregion

    #region ValidateFriendSignature Tests

    [Fact]
    public void ValidateFriendSignature_ValidSignature_ReturnsTrue()
    {
        var uid = "user123";
        var friendUid = "friend456";
        var timestamp = "1234567890";
        var expectedSig = SigUtils.CalcSignature(timestamp + "_" + friendUid + "_" + uid, TestSecret);
        
        var result = SigUtils.ValidateFriendSignature(uid, timestamp, friendUid, TestSecret, expectedSig);
        
        Assert.True(result);
    }

    [Fact]
    public void ValidateFriendSignature_InvalidSignature_ReturnsFalse()
    {
        var uid = "user123";
        var friendUid = "friend456";
        var timestamp = "1234567890";
        
        var result = SigUtils.ValidateFriendSignature(uid, timestamp, friendUid, TestSecret, "invalid");
        
        Assert.False(result);
    }

    #endregion

    #region CalcOAuth1Basestring Tests

    [Fact]
    public void CalcOAuth1Basestring_NormalizesUrl()
    {
        var url = "HTTPS://API.EXAMPLE.COM/path";
        var params_ = new GSObject();
        params_.Put("key", "value");
        
        var basestring = SigUtils.CalcOAuth1Basestring("POST", url, params_);
        
        Assert.Contains("https%3A%2F%2Fapi.example.com%2Fpath", basestring);
    }

    [Fact]
    public void CalcOAuth1Basestring_IncludesHttpMethod()
    {
        var url = "https://api.example.com/path";
        var params_ = new GSObject();
        
        var basestring = SigUtils.CalcOAuth1Basestring("POST", url, params_);
        
        Assert.StartsWith("POST&", basestring);
    }

    [Fact]
    public void CalcOAuth1Basestring_IncludesParameters()
    {
        var url = "https://api.example.com/path";
        var params_ = new GSObject();
        params_.Put("apiKey", "test123");
        params_.Put("format", "json");
        
        var basestring = SigUtils.CalcOAuth1Basestring("POST", url, params_);
        
        Assert.Contains("apiKey", basestring);
        Assert.Contains("test123", basestring);
        Assert.Contains("format", basestring);
        Assert.Contains("json", basestring);
    }

    [Fact]
    public void CalcOAuth1Basestring_SameInputProducesSameOutput()
    {
        var url = "https://api.example.com/path";
        var params1 = new GSObject();
        params1.Put("key", "value");
        var params2 = new GSObject();
        params2.Put("key", "value");
        
        var basestring1 = SigUtils.CalcOAuth1Basestring("POST", url, params1);
        var basestring2 = SigUtils.CalcOAuth1Basestring("POST", url, params2);
        
        Assert.Equal(basestring1, basestring2);
    }

    #endregion

    #region GetDynamicSessionSignature Tests

    [Fact]
    public void GetDynamicSessionSignature_ReturnsValidFormat()
    {
        var gltCookie = "test_login_token";
        var timeout = 300;
        
        var sig = SigUtils.GetDynamicSessionSignature(gltCookie, timeout, TestSecret);
        
        // Format: <expiration>_<signature>
        var parts = sig.Split('_');
        Assert.Equal(2, parts.Length);
        
        // First part should be a number (expiration timestamp)
        Assert.True(long.TryParse(parts[0], out var expiration));
        Assert.True(expiration > 0);
    }

    [Fact]
    public void GetDynamicSessionSignature_ExpirationIsInFuture()
    {
        var gltCookie = "test_login_token";
        var timeout = 300;
        
        var sig = SigUtils.GetDynamicSessionSignature(gltCookie, timeout, TestSecret);
        var parts = sig.Split('_');
        var expiration = long.Parse(parts[0]);
        var currentTime = SigUtils.CurrentTimeMillis() / 1000;
        
        Assert.True(expiration > currentTime);
        Assert.True(expiration <= currentTime + timeout + 5); // Allow 5 seconds tolerance
    }

    #endregion

    #region GetDynamicSessionSignatureUserSigned Tests

    [Fact]
    public void GetDynamicSessionSignatureUserSigned_ReturnsValidFormat()
    {
        var gltCookie = "test_login_token";
        var timeout = 300;
        var userKey = "userkey123";
        
        var sig = SigUtils.GetDynamicSessionSignatureUserSigned(gltCookie, timeout, userKey, TestSecret);
        
        // Format: <expiration>_<userKey>_<signature>
        var parts = sig.Split('_');
        Assert.Equal(3, parts.Length); // expiration_userkey123_signature
        
        // First part should be a number (expiration timestamp)
        Assert.True(long.TryParse(parts[0], out var expiration));
        Assert.True(expiration > 0);
        
        // Second part should be the user key
        Assert.Equal(userKey, parts[1]);
        
        // Should contain user key
        Assert.Contains(userKey, sig);
    }

    #endregion

    #region CurrentTimeMillis/CurrentTimeSeconds Tests

    [Fact]
    public void CurrentTimeMillis_ReturnsReasonableValue()
    {
        var millis = SigUtils.CurrentTimeMillis();
        
        // Should be after year 2020 (1577836800000 ms since epoch)
        Assert.True(millis > 1577836800000L);
        
        // Should be before year 2100 (4102444800000 ms since epoch)
        Assert.True(millis < 4102444800000L);
    }

    [Fact]
    public void CurrentTimeSeconds_ReturnsReasonableValue()
    {
        var seconds = SigUtils.CurrentTimeSeconds();
        
        // Should be after year 2020 (1577836800 seconds since epoch)
        Assert.True(seconds > 1577836800);
        
        // Should be before year 2100 (4102444800 seconds since epoch)
        Assert.True(seconds < 4102444800);
    }

    [Fact]
    public void CurrentTimeMillis_IsConsistentWithCurrentTimeSeconds()
    {
        var millis = SigUtils.CurrentTimeMillis();
        var seconds = SigUtils.CurrentTimeSeconds();
        
        // Millis / 1000 should be approximately equal to seconds
        Assert.True(Math.Abs(millis / 1000 - seconds) <= 1);
    }

    #endregion
}