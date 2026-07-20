/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - SigUtils
 */

using System.Security.Cryptography;
using System.Text;
using Gigya.Socialize.SDK.Internals;

namespace Gigya.Socialize.SDK;

/// <summary>
/// Utility class with static methods for calculating and validating cryptographic signatures.
/// </summary>
public static class SigUtils
{
    #region User Signature Validation

    /// <summary>
    /// Validates the authenticity of a socialize.getUserInfo API response.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    /// <param name="uid">The UID field from the response.</param>
    /// <param name="timestamp">The signatureTimestamp field from the response.</param>
    /// <param name="secret">Your partner's Secret Key (Base64 encoded).</param>
    /// <param name="signature">The UIDSignature field from the response.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool ValidateUserSignature(string uid, string timestamp, string secret, string signature)
    {
        var expectedSig = CalcSignature(timestamp + "_" + uid, secret);
        return ConstantTimeEquals(expectedSig, signature);
    }

    /// <summary>
    /// Validates the authenticity of a socialize.getUserInfo API response with expiration check.
    /// </summary>
    /// <param name="uid">The UID field from the response.</param>
    /// <param name="timestamp">The signatureTimestamp field from the response.</param>
    /// <param name="secret">Your partner's Secret Key (Base64 encoded).</param>
    /// <param name="signature">The UIDSignature field from the response.</param>
    /// <param name="expiration">The signature expiration time in seconds.</param>
    /// <returns>True if the signature is valid and not expired, false otherwise.</returns>
    public static bool ValidateUserSignature(string uid, string timestamp, string secret, string signature, int expiration)
    {
        return !SignatureTimestampExpired(timestamp, expiration) 
               && ValidateUserSignature(uid, timestamp, secret, signature);
    }

    #endregion

    #region Friend Signature Validation

    /// <summary>
    /// Validates the authenticity of a socialize.getFriendsInfo API response.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    /// <param name="uid">The UID field from the response.</param>
    /// <param name="timestamp">The signatureTimestamp field from the response.</param>
    /// <param name="friendUid">The friend's UID.</param>
    /// <param name="secret">Your partner's Secret Key (Base64 encoded).</param>
    /// <param name="signature">The friendshipSignature field from the response.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool ValidateFriendSignature(string uid, string timestamp, string friendUid, string secret, string signature)
    {
        var expectedSig = CalcSignature(timestamp + "_" + friendUid + "_" + uid, secret);
        return ConstantTimeEquals(expectedSig, signature);
    }

    /// <summary>
    /// Validates the authenticity of a socialize.getFriendsInfo API response with expiration check.
    /// </summary>
    /// <param name="uid">The UID field from the response.</param>
    /// <param name="timestamp">The signatureTimestamp field from the response.</param>
    /// <param name="friendUid">The friend's UID.</param>
    /// <param name="secret">Your partner's Secret Key (Base64 encoded).</param>
    /// <param name="signature">The friendshipSignature field from the response.</param>
    /// <param name="expiration">The signature expiration time in seconds.</param>
    /// <returns>True if the signature is valid and not expired, false otherwise.</returns>
    public static bool ValidateFriendSignature(string uid, string timestamp, string friendUid, string secret, string signature, int expiration)
    {
        return !SignatureTimestampExpired(timestamp, expiration) 
               && ValidateFriendSignature(uid, timestamp, friendUid, secret, signature);
    }

    #endregion

    #region Signature Calculation

    /// <summary>
    /// Generates a cryptographic signature using HMAC-SHA1.
    /// </summary>
    /// <param name="text">The string to sign.</param>
    /// <param name="key">The signing key (Base64 encoded).</param>
    /// <returns>The Base64 encoded signature.</returns>
    public static string CalcSignature(string text, string key)
    {
        var data = Encoding.UTF8.GetBytes(text);
        var keyData = Convert.FromBase64String(key);

        using var hmac = new HMACSHA1(keyData);
        var hash = hmac.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Generates the OAuth1 base string for signature calculation.
    /// </summary>
    /// <param name="httpMethod">The HTTP method ("POST" or "GET").</param>
    /// <param name="url">The full URL without query parameters.</param>
    /// <param name="requestParams">The request parameters as a GSObject.</param>
    /// <returns>The OAuth1 base string.</returns>
    public static string CalcOAuth1Basestring(string httpMethod, string url, GSObject requestParams)
    {
        // Normalize the URL per OAuth requirements
        var normalizedUrl = new StringBuilder();
        var uri = new Uri(url);

        normalizedUrl.Append(uri.Scheme.ToLowerInvariant());
        normalizedUrl.Append("://");
        normalizedUrl.Append(uri.Host.ToLowerInvariant());
        
        if ((uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && uri.Port != 80) ||
            (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && uri.Port != 443))
        {
            normalizedUrl.Append(':');
            normalizedUrl.Append(uri.Port);
        }
        
        normalizedUrl.Append(uri.LocalPath);

        // Create a sorted list of query parameters
        var querystring = new StringBuilder();

        foreach (var key in requestParams.GetKeys())
        {
            var value = requestParams.GetString(key, null);
            if (value != null)
            {
                querystring.Append(key);
                querystring.Append('=');
                querystring.Append(GSRequest.UrlEncode(value));
                querystring.Append('&');
            }
        }
        
        if (querystring.Length > 0)
            querystring.Length--; // Remove the last ampersand

        // Construct the base string
        var baseString = httpMethod.ToUpperInvariant() + "&" +
                        GSRequest.UrlEncode(normalizedUrl.ToString()) + "&" +
                        GSRequest.UrlEncode(querystring.ToString());
        
        return baseString;
    }

    #endregion

    #region Dynamic Session Signatures

    /// <summary>
    /// Generates a dynamic session signature for cookie-based authentication.
    /// </summary>
    /// <param name="gltCookie">The login token cookie value.</param>
    /// <param name="timeoutInSeconds">The session timeout in seconds.</param>
    /// <param name="secret">The secret key (Base64 encoded).</param>
    /// <returns>The dynamic session signature.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if timeoutInSeconds is negative.</exception>
    public static string GetDynamicSessionSignature(string gltCookie, int timeoutInSeconds, string secret)
    {
        if (timeoutInSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutInSeconds), "Timeout cannot be negative");
        
        // Use checked arithmetic to prevent overflow
        var currentTimeSeconds = CurrentTimeMillis() / 1000;
        var expirationTimeUnix = checked(currentTimeSeconds + timeoutInSeconds).ToString();
        var unsignedExpString = gltCookie + "_" + expirationTimeUnix;
        var signedExpString = CalcSignature(unsignedExpString, secret);
        return expirationTimeUnix + "_" + signedExpString;
    }

    /// <summary>
    /// Generates a user-signed dynamic session signature for cookie-based authentication.
    /// </summary>
    /// <param name="gltCookie">The login token cookie value.</param>
    /// <param name="timeoutInSeconds">The session timeout in seconds.</param>
    /// <param name="userKey">The user key.</param>
    /// <param name="secret">The secret key (Base64 encoded).</param>
    /// <returns>The dynamic session signature.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if timeoutInSeconds is negative.</exception>
    public static string GetDynamicSessionSignatureUserSigned(string gltCookie, int timeoutInSeconds, string userKey, string secret)
    {
        if (timeoutInSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutInSeconds), "Timeout cannot be negative");
        
        // Use checked arithmetic to prevent overflow
        var currentTimeSeconds = CurrentTimeMillis() / 1000;
        var expirationTimeUnix = checked(currentTimeSeconds + timeoutInSeconds).ToString();
        var unsignedExpString = gltCookie + "_" + expirationTimeUnix + "_" + userKey;
        var signedExpString = CalcSignature(unsignedExpString, secret);
        return expirationTimeUnix + "_" + userKey + "_" + signedExpString;
    }

    #endregion

    #region JWT Authorization

    /// <summary>
    /// Calculates the Authorization Bearer header value using JWT with RS256.
    /// </summary>
    /// <param name="userKey">The user key (used as 'kid' in JWT header).</param>
    /// <param name="privateKey">The RSA private key in PEM format.</param>
    /// <returns>The Authorization header value (e.g., "Bearer eyJ...").</returns>
    public static string CalcAuthorizationBearer(string userKey, string privateKey)
    {
        const string algorithm = "RS256";
        const string jwtType = "JWT";

        var header = new GSObject(new
        {
            alg = algorithm,
            typ = jwtType,
            kid = userKey
        });

        var epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var issued = (long)DateTime.UtcNow.Subtract(epochTime).TotalSeconds;
        
        var payload = new GSObject(new
        {
            iat = issued,
            jti = Guid.NewGuid().ToString()
        });

        var headerBytes = Encoding.UTF8.GetBytes(header.ToJsonString());
        var payloadBytes = Encoding.UTF8.GetBytes(payload.ToJsonString());

        var baseString = Base64UrlEncode(headerBytes) + "." + Base64UrlEncode(payloadBytes);

        using var rsa = DecodeRsaPrivateKey(privateKey);
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(baseString), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signatureString = Base64UrlEncode(signature);

        return "Bearer " + baseString + "." + signatureString;
    }

    #endregion

    #region JWT Validation

    /// <summary>
    /// Validates a JWT signature and 'issued at' timestamp.
    /// </summary>
    /// <param name="jwt">The JWT token.</param>
    /// <param name="apiDomain">The API domain the JWT was obtained from (e.g., "us1.gigya.com").</param>
    /// <returns>A dictionary of claims if validation succeeds, null otherwise.</returns>
    public static IDictionary<string, object>? ValidateSignature(string jwt, string apiDomain)
    {
        return JwtUtils.ValidateSignature(jwt, apiDomain);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Performs a constant-time comparison of two strings to prevent timing attacks.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value to compare.</param>
    /// <returns>True if the strings are equal, false otherwise.</returns>
    private static bool ConstantTimeEquals(string expected, string actual)
    {
        if (expected == null || actual == null)
            return expected == actual;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    internal static long CurrentTimeMillis()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    internal static int CurrentTimeSeconds()
    {
        return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    private static bool SignatureTimestampExpired(string signatureTimestamp, int expiration)
    {
        try
        {
            var timestamp = Convert.ToInt32(signatureTimestamp);
            return Math.Abs(CurrentTimeSeconds() - timestamp) > expiration;
        }
        catch
        {
            return true;
        }
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static RSA DecodeRsaPrivateKey(string privateKey)
    {
        const string keyHeader = "-----BEGIN RSA PRIVATE KEY-----";
        const string keyFooter = "-----END RSA PRIVATE KEY-----";
        const string pkcs8Header = "-----BEGIN PRIVATE KEY-----";
        const string pkcs8Footer = "-----END PRIVATE KEY-----";

        var rsa = RSA.Create();
        
        // Try PKCS#8 format first
        if (privateKey.Contains(pkcs8Header))
        {
            var keyData = privateKey
                .Replace(pkcs8Header, "")
                .Replace(pkcs8Footer, "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(" ", "");
            
            var keyBytes = Convert.FromBase64String(keyData);
            rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            return rsa;
        }
        
        // Try PKCS#1 format
        if (privateKey.Contains(keyHeader))
        {
            var keyData = privateKey
                .Replace(keyHeader, "")
                .Replace(keyFooter, "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(" ", "");
            
            var keyBytes = Convert.FromBase64String(keyData);
            rsa.ImportRSAPrivateKey(keyBytes, out _);
            return rsa;
        }

        throw new ArgumentException("Unsupported private key format. Expected PKCS#1 or PKCS#8 PEM format.");
    }

    #endregion
}