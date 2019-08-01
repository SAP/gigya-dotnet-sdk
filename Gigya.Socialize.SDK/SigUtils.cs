/*
 * Copyright (C) 2011 Gigya, Inc.
 * Version 2.16.0
 */

using System;
using System.Text;
using System.Security.Cryptography;
using Gigya.Socialize.SDK.Internals;

namespace Gigya.Socialize.SDK 
{
    /// <summary>
    /// This class is a utility class with static methods for calculating and validating cryptographic signatures.
    /// </summary>
    /// <remarks>author Raviv Pavel</remarks>
    public class SigUtils
    {
        /// <summary>
        /// Use this method to verify the authenticity of a 
        /// <a href="https://developers.gigya.com/display/GD/socialize.getUserInfo+REST">socialize.getUserInfo</a> API method response,
        /// to make sure it is in fact originating from Gigya, and prevent fraud. 
        /// The "socialize.getUserInfo" API method response data include the following fields: 
        /// UID, signatureTimestamp (a timestamp) and UIDSignature (a cryptographic signature).
        /// Pass these fields as the corresponding parameters of this method, along with your partner's "Secret Key".
        /// Your secret key (provided in BASE64 encoding) is located at the bottom of the 
        /// <a href="https://console.gigya.com/site/partners/wfsocapi.aspx#&amp;&amp;userstate=SiteSetup">Site Setup</a> page on Gigya's website.
        /// The return value of the method indicates if the signature is valid (thus, originating from Gigya) or not.
        /// </summary>
        /// <param name="UID">pass the UID field returned by the "socialize.getUserInfo" API method response </param>
        /// <param name="timestamp">pass the signatureTimestamp field returned by the "socialize.getUserInfo" API method response </param>
        /// <param name="secret">your partner's "Secret Key", obtained from Gigya's website.</param>
        /// <param name="signature">pass the UIDSignature field returned by the "socialize.getUserInfo" API method response</param>
        /// <returns></returns>
        public static bool ValidateUserSignature(string UID, string timestamp, string secret, string signature)
        {
            string expectedSig = CalcSignature(timestamp + "_" + UID, secret);
            return expectedSig.Equals(signature);
        }

        /// <summary>
        /// Use this method to verify the authenticity of a 
        /// <a href="https://developers.gigya.com/display/GD/socialize.getFriendsInfo+REST">socialize.getFriendsInfo</a> API 
        /// method response, to make sure it is in fact originating from Gigya, and prevent fraud. 
        /// The "socialize.getFriendsInfo" API method response data include the following fields: 
        /// UID, signatureTimestamp (a timestamp) and friendshipSignature (a cryptographic signature).
        /// Pass these fields as the corresponding parameters of this method, along with your partner's "Secret Key". Your secret 
        /// key (provided in BASE64 encoding) is located at the bottom of the 
        /// <a href="https://console.gigya.com/site/partners/wfsocapi.aspx#&amp;&amp;userstate=SiteSetup">Site Setup</a> page on Gigya's website.
        /// The return value of the method indicates if the signature is valid (thus, originating from Gigya) or not.
        /// </summary>
        /// <param name="UID">pass the UID field returned by the "socialize.getFriendsInfo" API method response </param>
        /// <param name="timestamp">pass the signatureTimestamp field returned by the "socialize.getFriendsInfo" API method response </param>
        /// <param name="friendUID"></param>
        /// <param name="secret">your partner's "Secret Key", obtained from Gigya's website.</param>
        /// <param name="signature">pass the friendshipSignature field returned by the "socialize.getFriendsInfo" API method response</param>
        /// <returns></returns>
        public static bool ValidateFriendSignature(string UID, string timestamp, string friendUID, string secret, string signature)
        {
            string expectedSig = CalcSignature(timestamp + "_" + friendUID + "_" + UID, secret);
            return expectedSig.Equals(signature);
        }

        /// <summary>
        /// This is a utility method for generating a cryptographic signature.
        /// </summary>
        /// <param name="text">the string for signing></param>
        /// <param name="key">the key for signing. Use your partner's "Secret Key", obtained from Gigya's website, as the signing key</param>
        /// <returns></returns>
        public static string CalcSignature(string text, string key)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] keyData = Convert.FromBase64String(key);

            // Compute signature for provided challenge and private key
            // Always use HMAC-SHA1 algorithm
            HMAC hmac = new HMACSHA1(keyData);

            byte[] macReciever = hmac.ComputeHash(data);
            return Convert.ToBase64String(macReciever);
        }

        /// <summary>
        /// This is a utility method for generating a base string for calculating the OAuth1 cryptographic signature.
        /// </summary>
        /// <param name="httpMethod">"POST" or "GET"</param>
        /// <param name="url">the full url without params</param>
        /// <param name="requestParams">list of params in the form of a GSObject</param>
        /// <returns>the base string to act on for calculating the OAuth1 cryptographic signature</returns>
        public static string CalcOAuth1Basestring(string httpMethod, string url, GSObject requestParams)
        {
            // Normalize the URL per the OAuth requirements
            StringBuilder normalizedUrlSB = new StringBuilder();
            Uri u = new Uri(url);

            normalizedUrlSB.Append(u.Scheme.ToLowerInvariant());
            normalizedUrlSB.Append("://");
            normalizedUrlSB.Append(u.Host.ToLowerInvariant());
            if ((u.Scheme == "HTTP" && u.Port != 80) || (u.Scheme == "HTTPS" && u.Port != 443))
            {
                normalizedUrlSB.Append(':');
                normalizedUrlSB.Append(u.Port);
            }
            normalizedUrlSB.Append(u.LocalPath);


            // Create a sorted list of query parameters
            StringBuilder querystring = new StringBuilder();
            
            foreach (string key in requestParams.GetKeys())
            {
                if (requestParams.GetString(key) != null)
                {
                    querystring.Append(key);
                    querystring.Append("=");
                    querystring.Append(GSRequest.UrlEncode(requestParams.GetString(key) ?? String.Empty));
                    querystring.Append("&");
                }
            }
            if (querystring.Length > 0) querystring.Length--;	// remove the last ampersand

            // Construct the base string from the HTTP method, the URL and the parameters 
            string basestring = 
                httpMethod.ToUpperInvariant() + "&" + 
                GSRequest.UrlEncode(normalizedUrlSB.ToString()) + "&" + 
                GSRequest.UrlEncode(querystring.ToString());
            return basestring;

        }

        internal static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public static string GetDynamicSessionSignature(string glt_cookie, int timeoutInSeconds, string secret)
        {
            // cookie format: 
            // <expiration time in unix time format>_BASE64(HMACSHA1(secret key, <login token>_<expiration time in unix time format>))

            string expirationTimeUnix = (CurrentTimeMillis() / 1000 + timeoutInSeconds).ToString();
            string unsignedExpString = glt_cookie + "_" + expirationTimeUnix;
            string signedExpString = CalcSignature(unsignedExpString, secret); // sign the base string using the secret key
            string ret = expirationTimeUnix + '_' + signedExpString;   // define the cookie value

            return ret;
        }

        public static string GetDynamicSessionSignatureUserSigned(string glt_cookie, int timeoutInSeconds, string userKey, string secret)
        {
            // cookie format: 
            // <expiration time in unix time format>_<User Key>_BASE64(HMACSHA1(secret key, <login token>_<expiration time in unix time format>_<User Key>))

            string expirationTimeUnix = (CurrentTimeMillis() / 1000 + timeoutInSeconds).ToString();
            string unsignedExpString = glt_cookie + "_" + expirationTimeUnix + "_" + userKey;
            string signedExpString = CalcSignature(unsignedExpString, secret); // sign the base string using the secret key
            string ret = expirationTimeUnix + "_" + userKey + "_" + signedExpString;   // define the cookie value

            return ret;
        }

        public static string CalcAuthorizationBearer(string userKey, string privateKey)
        {
            const string algorithm = "RS256";
            const string jwtName = "JWT";
            const string hAlg = "SHA256";
            
            var header = new GSObject(new
            {
                alg = algorithm,
                typ = jwtName,
                kid = userKey                
            });

            var epochTime = new DateTime(1970, 1, 1);
            var issued = (long)DateTime.UtcNow.Subtract(epochTime).TotalSeconds;
            var payload = new GSObject(new
            {
                iat = issued,
                jti = Guid.NewGuid().ToString()
            });
            
            var headerBytes = Encoding.UTF8.GetBytes(header.ToJsonString());
            var payloadBytes = Encoding.UTF8.GetBytes(payload.ToJsonString());

            var baseString = string.Join(".",
                new[] {Convert.ToBase64String(headerBytes), Convert.ToBase64String(payloadBytes)});

            var rsa = RsaUtils.DecodeRsaPrivateKey(privateKey);

            var signature = rsa.SignData(Encoding.UTF8.GetBytes(baseString), hAlg);

            var signatureString = Convert.ToBase64String(signature);
            
            return "Bearer " + string.Join(".", new []{ baseString, signatureString});
        }
    }
}
