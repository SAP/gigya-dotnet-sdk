using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Gigya.Socialize.SDK.Internals
{
    internal class JwtHeader
    {
        public string kid { get; set; }
    }

    internal class PublicKeyParams
    {
        public string n { get; set; }
        public string e { get; set; }
    }

    internal class JwtUtils
    {
        private static readonly JavaScriptSerializer _deserializer = new JavaScriptSerializer();

        private static readonly Dictionary<string, KeyValuePair<string, DateTime>> _publicKeysCache = new Dictionary<string, KeyValuePair<string, DateTime>>(StringComparer.InvariantCultureIgnoreCase);

        private static T Deserialize<T>(string sourceBase64) => _deserializer.Deserialize<T>(sourceBase64.FromBase64UrlString().GetString());

        private static T TryButNotTooHard<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch
            {
                return default(T);
            }
        }

        private static bool IsTimestampValid(int timestamp, int allowDiffSec)
        {
            var unixTimeStartUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var offset = DateTime.UtcNow - unixTimeStartUtc.AddSeconds(timestamp);
            return Math.Abs(offset.TotalSeconds) < allowDiffSec;
        }

        private static RSACryptoServiceProvider RSAFromKeyParams(string jwk)
        {
            try
            {
                var jPubKey = _deserializer.Deserialize<PublicKeyParams>(jwk);
                var n = jPubKey.n.FromBase64UrlString();
                var e = jPubKey.e.FromBase64UrlString();
                var rsa = new RSACryptoServiceProvider();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = n,
                    Exponent = e
                });
                return rsa;
            }
            catch
            {
                // ignored
            }

            return null;
        }

        /// <summary>
        /// Fetch available public key representation validated by the "kid".
        /// </summary>
        /// <param name="kid">The keyId</param>
        /// <param name="apiDomain">The api domain jwt was obtained, for example us1.gigya.com</param>
        private static string FetchPublicKey(string kid, string apiDomain)
        {
            var resourceUri = $"https://accounts.{apiDomain}/accounts.getJWTPublicKey?V2=true";
            var request = (HttpWebRequest)WebRequest.Create(resourceUri);
            request.Timeout = 30_000;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.Method = "GET";
            request.KeepAlive = false;
            request.ServicePoint.Expect100Continue = false;

            GSResponse response;
            using (var webResponse = (HttpWebResponse)request.GetResponse())
            using (var sr = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                response = new GSResponse(request.Method, headers: null, sr.ReadToEnd(), logSoFar: null);

            if (response.GetErrorCode() == 0)
            {
                GSArray keys = response.GetArray("keys", null);
                if (keys == null || keys.Length == 0)
                {
                    // Failed to obtain JWK from response data OR data is empty
                    return null;
                }

                foreach (object key in keys)
                    if (key is GSObject)
                    {
                        string jwtKid = ((GSObject)key).GetString("kid", null);
                        if (jwtKid != null && jwtKid == kid)
                            return ((GSObject)key).ToJsonString();
                    }
            }

            return null;
        }

        /// <summary>
        /// Validate JWT signature and expiration.
        /// Pay attention, the public key is cached for 24 hours by kid.
        /// </summary>
        /// <param name="jwt"></param>
        /// <param name="apiDomain">The api domain jwt was obtained, for example us1.gigya.com</param>
        public static IDictionary<string, object> ValidateSignature(string jwt, string apiDomain)
        {
            var segments = jwt.Split('.');

            if (segments.Length != 3)
                return null;

            var jwtHeader = TryButNotTooHard(() => Deserialize<JwtHeader>(segments[0]));

            string kid = jwtHeader?.kid;

            if (kid == null)
                return null;

            string publicJWK = null;

            // Try to fetch from cache, check isn't too old, fetch again if
            if (_publicKeysCache.ContainsKey(kid))
            {
                var pair = _publicKeysCache[kid];
                if (DateTime.UtcNow - pair.Value < TimeSpan.FromDays(1))
                    publicJWK = pair.Key;
            }

            if (publicJWK == null)
                publicJWK = FetchPublicKey(kid, apiDomain);

            if (publicJWK == null)
                return null;

            using (var rsa = RSAFromKeyParams(publicJWK))
            {
                if (rsa == null)
                    return null; // Failed to instantiate PublicKey instance from jwk

                _publicKeysCache[kid] = new KeyValuePair<string, DateTime>(key: publicJWK, value: DateTime.UtcNow);

                var data = Encoding.UTF8.GetBytes(segments[0] + '.' + segments[1]);
                var signature = segments[2].FromBase64UrlString();

                var valid = rsa.VerifyData(data, "SHA256", signature);

                if (!valid)
                    return null; // Failed to validate the jwt signature
            }

            var claims = Deserialize<Dictionary<string, object>>(segments[1]);

            if (!IsTimestampValid((int)claims["iat"], 60 * 2))
                return null; // Failed to validate the jwt token issued at

            return claims;
        }
    }
}
