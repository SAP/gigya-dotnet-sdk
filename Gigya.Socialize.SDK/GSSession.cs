/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GSSession
 */

namespace Gigya.Socialize.SDK;

/// <summary>
/// Wraps a Gigya session returned from the OAuth2 login process.
/// </summary>
public class GSSession
{
    private string? _secret;
    private string? _accessToken;
    private long _expirationTime;

    #region Constructors

    /// <summary>
    /// Default constructor.
    /// </summary>
    public GSSession()
    {
    }

    /// <summary>
    /// Constructs a session with the specified parameters.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="secret">The session secret.</param>
    /// <param name="expirationSeconds">The expiration time in seconds from now (0 = never expires).</param>
    public GSSession(string? accessToken, string? secret, long expirationSeconds)
    {
        setAccessToken(accessToken);
        setSecret(secret);
        
        if (expirationSeconds == 0)
            setExpirationTime(long.MaxValue);
        else
            setExpirationTime(CurrentTimeMillis() + (1000 * expirationSeconds));
    }

    /// <summary>
    /// Constructs a session from a GSObject containing session parameters.
    /// </summary>
    /// <param name="sessionParams">A GSObject containing access_token, access_token_secret, and expires_in.</param>
    public GSSession(GSObject sessionParams)
        : this(
            sessionParams.GetString("access_token", null),
            sessionParams.GetString("access_token_secret", null),
            sessionParams.GetLong("expires_in", 0))
    {
    }

    #endregion

    #region Properties (Legacy Java-style getters/setters for compatibility)

    /// <summary>
    /// Sets the session secret.
    /// </summary>
    /// <param name="secret">The secret value.</param>
    [Obsolete("Use the Secret property instead.")]
    public void setSecret(string? secret) => _secret = secret;

    /// <summary>
    /// Gets the session secret.
    /// </summary>
    /// <returns>The secret value.</returns>
    [Obsolete("Use the Secret property instead.")]
    public string? getSecret() => _secret;

    /// <summary>
    /// Sets the access token.
    /// </summary>
    /// <param name="accessToken">The access token value.</param>
    [Obsolete("Use the AccessToken property instead.")]
    public void setAccessToken(string? accessToken) => _accessToken = accessToken;

    /// <summary>
    /// Gets the access token.
    /// </summary>
    /// <returns>The access token value.</returns>
    [Obsolete("Use the AccessToken property instead.")]
    public string? getAccessToken() => _accessToken;

    /// <summary>
    /// Sets the expiration time.
    /// </summary>
    /// <param name="expirationTime">The expiration time in milliseconds since epoch.</param>
    [Obsolete("Use the ExpirationTime property instead.")]
    public void setExpirationTime(long expirationTime) => _expirationTime = expirationTime;

    /// <summary>
    /// Gets the expiration time.
    /// </summary>
    /// <returns>The expiration time in milliseconds since epoch.</returns>
    [Obsolete("Use the ExpirationTime property instead.")]
    public long getExpirationTime() => _expirationTime;

    #endregion

    #region Modern Properties

    /// <summary>
    /// Gets or sets the session secret.
    /// </summary>
    public string? Secret
    {
        get => _secret;
        set => _secret = value;
    }

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    public string? AccessToken
    {
        get => _accessToken;
        set => _accessToken = value;
    }

    /// <summary>
    /// Gets or sets the expiration time in milliseconds since epoch.
    /// </summary>
    public long ExpirationTime
    {
        get => _expirationTime;
        set => _expirationTime = value;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Checks if the session is valid (has an access token and hasn't expired).
    /// </summary>
    /// <returns>True if the session is valid, false otherwise.</returns>
    public bool IsValid()
    {
        return getAccessToken() != null && CurrentTimeMillis() < getExpirationTime();
    }

    /// <summary>
    /// Gets the current time in milliseconds since Unix epoch.
    /// </summary>
    /// <returns>The current time in milliseconds.</returns>
    public static long CurrentTimeMillis()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    #endregion
}