/*
 * Copyright (C) 2024 SAP SE
 * Characterization tests for GSSession
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Characterization tests for GSSession to verify behavior matches legacy SDK.
/// </summary>
public class GSSessionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptySession()
    {
        var session = new GSSession();
        
        Assert.Null(session.AccessToken);
        Assert.Null(session.Secret);
        Assert.Equal(0, session.ExpirationTime);
    }

    [Fact]
    public void Constructor_WithParameters_SetsValues()
    {
        var session = new GSSession("test-token", "test-secret", 3600);
        
        Assert.Equal("test-token", session.AccessToken);
        Assert.Equal("test-secret", session.Secret);
    }

    [Fact]
    public void Constructor_WithZeroExpiration_SetsMaxValue()
    {
        var session = new GSSession("test-token", "test-secret", 0);
        
        Assert.Equal(long.MaxValue, session.ExpirationTime);
    }

    [Fact]
    public void Constructor_WithPositiveExpiration_CalculatesExpirationTime()
    {
        var beforeTime = GSSession.CurrentTimeMillis();
        var session = new GSSession("test-token", "test-secret", 3600);
        var afterTime = GSSession.CurrentTimeMillis();
        
        // Expiration should be approximately 3600 seconds (3600000 ms) from now
        var expectedMin = beforeTime + (3600 * 1000);
        var expectedMax = afterTime + (3600 * 1000);
        
        Assert.InRange(session.ExpirationTime, expectedMin, expectedMax);
    }

    [Fact]
    public void Constructor_FromGSObject_ParsesCorrectly()
    {
        var sessionParams = new GSObject();
        sessionParams.Put("access_token", "test-token");
        sessionParams.Put("access_token_secret", "test-secret");
        sessionParams.Put("expires_in", 3600L);
        
        var session = new GSSession(sessionParams);
        
        Assert.Equal("test-token", session.AccessToken);
        Assert.Equal("test-secret", session.Secret);
    }

    [Fact]
    public void Constructor_FromGSObject_WithMissingValues_UsesDefaults()
    {
        var sessionParams = new GSObject();
        
        var session = new GSSession(sessionParams);
        
        Assert.Null(session.AccessToken);
        Assert.Null(session.Secret);
        Assert.Equal(long.MaxValue, session.ExpirationTime); // 0 expires_in = never expires
    }

    #endregion

    #region Property Tests

    [Fact]
    public void AccessToken_CanBeSetAndRetrieved()
    {
        var session = new GSSession();
        
        session.AccessToken = "new-token";
        
        Assert.Equal("new-token", session.AccessToken);
    }

    [Fact]
    public void Secret_CanBeSetAndRetrieved()
    {
        var session = new GSSession();
        
        session.Secret = "new-secret";
        
        Assert.Equal("new-secret", session.Secret);
    }

    [Fact]
    public void ExpirationTime_CanBeSetAndRetrieved()
    {
        var session = new GSSession();
        
        session.ExpirationTime = 1234567890L;
        
        Assert.Equal(1234567890L, session.ExpirationTime);
    }

    #endregion

    #region Legacy Getter/Setter Tests

    [Fact]
    public void LegacyGettersSetters_WorkCorrectly()
    {
        var session = new GSSession();
        
#pragma warning disable CS0618 // Type or member is obsolete
        session.setAccessToken("legacy-token");
        session.setSecret("legacy-secret");
        session.setExpirationTime(9876543210L);
        
        Assert.Equal("legacy-token", session.getAccessToken());
        Assert.Equal("legacy-secret", session.getSecret());
        Assert.Equal(9876543210L, session.getExpirationTime());
#pragma warning restore CS0618
    }

    [Fact]
    public void LegacyAndModernProperties_AreSynchronized()
    {
        var session = new GSSession();
        
        // Set via modern property
        session.AccessToken = "modern-token";
        
#pragma warning disable CS0618 // Type or member is obsolete
        // Read via legacy getter
        Assert.Equal("modern-token", session.getAccessToken());
        
        // Set via legacy setter
        session.setSecret("legacy-secret");
#pragma warning restore CS0618
        
        // Read via modern property
        Assert.Equal("legacy-secret", session.Secret);
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_WithValidTokenAndFutureExpiration_ReturnsTrue()
    {
        var session = new GSSession("test-token", "test-secret", 3600);
        
        Assert.True(session.IsValid());
    }

    [Fact]
    public void IsValid_WithNullToken_ReturnsFalse()
    {
        var session = new GSSession(null, "test-secret", 3600);
        
        Assert.False(session.IsValid());
    }

    [Fact]
    public void IsValid_WithExpiredSession_ReturnsFalse()
    {
        var session = new GSSession();
        session.AccessToken = "test-token";
        session.ExpirationTime = GSSession.CurrentTimeMillis() - 1000; // 1 second ago
        
        Assert.False(session.IsValid());
    }

    [Fact]
    public void IsValid_WithNeverExpires_ReturnsTrue()
    {
        var session = new GSSession("test-token", "test-secret", 0);
        
        Assert.True(session.IsValid());
    }

    #endregion

    #region CurrentTimeMillis Tests

    [Fact]
    public void CurrentTimeMillis_ReturnsReasonableValue()
    {
        var currentTime = GSSession.CurrentTimeMillis();
        
        // Should be after year 2020 (1577836800000 ms since epoch)
        Assert.True(currentTime > 1577836800000L);
        
        // Should be before year 2100 (4102444800000 ms since epoch)
        Assert.True(currentTime < 4102444800000L);
    }

    [Fact]
    public void CurrentTimeMillis_IsIncreasing()
    {
        var time1 = GSSession.CurrentTimeMillis();
        Thread.Sleep(10);
        var time2 = GSSession.CurrentTimeMillis();
        
        Assert.True(time2 >= time1);
    }

    [Fact]
    public void CurrentTimeMillis_IsConsistentWithDateTime()
    {
        var sessionTime = GSSession.CurrentTimeMillis();
        var dotnetTime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        
        // Should be within 1 second of each other
        Assert.InRange(Math.Abs(sessionTime - dotnetTime), 0, 1000);
    }

    #endregion
}