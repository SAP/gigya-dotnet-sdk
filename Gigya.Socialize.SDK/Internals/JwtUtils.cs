/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - JwtUtils
 */

using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gigya.Socialize.SDK.Internals;

/// <summary>
/// Internal utilities for JWT validation.
/// </summary>
internal static class JwtUtils
{
    private static readonly HttpClient HttpClient = new();
    private static readonly Dictionary<string, CachedKey> KeyCache = new();
    private static readonly object CacheLock = new();

    /// <summary>
    /// Validates a JWT signature and 'issued at' timestamp.
    /// </summary>
    /// <param name="jwt">The JWT token.</param>
    /// <param name="apiDomain">The API domain the JWT was obtained from.</param>
    /// <returns>A dictionary of claims if validation succeeds, null otherwise.</returns>
    public static IDictionary<string, object>? ValidateSignature(string jwt, string apiDomain)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return null;

            var headerJson = Base64UrlDecode(parts[0]);
            var payloadJson = Base64UrlDecode(parts[1]);
            var signature = Base64UrlDecodeBytes(parts[2]);

            var header = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(headerJson);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);

            if (header == null || payload == null)
                return null;

            // Get the key ID from header
            if (!header.TryGetValue("kid", out var kidElement))
                return null;
            var kid = kidElement.GetString();
            if (string.IsNullOrEmpty(kid))
                return null;

            // Get the algorithm
            if (!header.TryGetValue("alg", out var algElement))
                return null;
            var alg = algElement.GetString();
            if (alg != "RS256")
                return null;

            // Get the public key
            var publicKey = GetPublicKey(apiDomain, kid);
            if (publicKey == null)
                return null;

            // Verify signature
            var dataToVerify = Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]);
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);

            if (!rsa.VerifyData(dataToVerify, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                return null;

            // Validate 'iat' (issued at) - should not be in the future
            if (payload.TryGetValue("iat", out var iatElement))
            {
                var iat = iatElement.GetInt64();
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (iat > now + 300) // Allow 5 minutes clock skew
                    return null;
            }

            // Convert payload to dictionary
            var claims = new Dictionary<string, object>();
            foreach (var kvp in payload)
            {
                claims[kvp.Key] = ConvertJsonElement(kvp.Value);
            }

            return claims;
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? GetPublicKey(string apiDomain, string kid)
    {
        var cacheKey = $"{apiDomain}:{kid}";

        lock (CacheLock)
        {
            if (KeyCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                return cached.Key;
            }
        }

        try
        {
            var jwksUrl = $"https://{apiDomain}/accounts.getJWTPublicKey?V2=true";
            var response = HttpClient.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
            var jwks = JsonSerializer.Deserialize<JsonElement>(response);

            if (jwks.TryGetProperty("keys", out var keys))
            {
                foreach (var key in keys.EnumerateArray())
                {
                    if (key.TryGetProperty("kid", out var keyKid) && keyKid.GetString() == kid)
                    {
                        if (key.TryGetProperty("n", out var n) && key.TryGetProperty("e", out var e))
                        {
                            var modulus = Base64UrlDecodeBytes(n.GetString()!);
                            var exponent = Base64UrlDecodeBytes(e.GetString()!);

                            using var rsa = RSA.Create();
                            rsa.ImportParameters(new RSAParameters
                            {
                                Modulus = modulus,
                                Exponent = exponent
                            });

                            var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();

                            lock (CacheLock)
                            {
                                KeyCache[cacheKey] = new CachedKey
                                {
                                    Key = publicKeyBytes,
                                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                                };
                            }

                            return publicKeyBytes;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    private static string Base64UrlDecode(string input)
    {
        var bytes = Base64UrlDecodeBytes(input);
        return Encoding.UTF8.GetString(bytes);
    }

    private static byte[] Base64UrlDecodeBytes(string input)
    {
        var output = input
            .Replace('-', '+')
            .Replace('_', '/');

        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }

        return Convert.FromBase64String(output);
    }

    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            _ => element.GetRawText()
        };
    }

    private class CachedKey
    {
        public byte[] Key { get; set; } = Array.Empty<byte>();
        public DateTime ExpiresAt { get; set; }
    }
}