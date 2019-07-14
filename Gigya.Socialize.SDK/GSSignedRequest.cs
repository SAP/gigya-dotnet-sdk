using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Gigya.Socialize.SDK.Internals;

namespace Gigya.Socialize.SDK
{
    public class GSSignedRequest : GSRequest
    {
        private readonly string _privateKey;

        private const string Algorithm = "RS256";

        private const string JwtName = "JWT";
        
        private const string H_alg = "SHA256";

        /// <summary>
        /// Constructs a request using an apiKey and secretKey.
        /// Suitable for calling our old REST API
        /// </summary>
        /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="userKey">An administrative user's key.</param>
        /// <param name="privateKey">An administrative user's private key (PEM). usually read from file</param>
        /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
        /// If namespaces is not supplied "socialize" is assumed</param>
        /// <param name="clientParams">The request parameters</param>
        /// <param name="additionalHeaders">A collection of additional headers for the HTTP request.</param>
        /// <param name="proxy">Proxy for the HTTP request.</param>
        public GSSignedRequest(string apiKey, string userKey, string privateKey, string apiMethod, object clientParams = null,
            NameValueCollection additionalHeaders = null, IWebProxy proxy = null)
            : base(apiKey, null, apiMethod, clientParams, true, userKey, additionalHeaders, proxy)
        {
            if (userKey == null)
            {
                Logger.Write(new MissingFieldException("Signed request must have userKey"));
                return;
            }
            _privateKey = privateKey;
        }

        protected override bool IsValidRequest()
        {
            return !string.IsNullOrEmpty(Method);
        }
        
        protected override void SetRequiredParamsAndSign(string httpMethod, string resourceUri)
        {
            if (ApiKey != null)
                SetParam("apiKey", ApiKey);

            Sign(httpMethod, resourceUri);
        }

        protected override void Sign(string httpMethod, string resourceUri)
        {
            if (UserKey == null)
            {
                Logger.Write(new MissingFieldException("Failed to sign request, missing userKey"));
                return;
            }
            
            var header = new GSObject(new
            {
                alg = Algorithm,
                typ = JwtName,
                kid = UserKey                
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

            var rsa = RsaUtils.DecodeRsaPrivateKey(_privateKey);

            var signature = rsa.SignData(Encoding.UTF8.GetBytes(baseString), H_alg);

            var signatureString = Convert.ToBase64String(signature);

            if (null == AdditionalHeaders)
            {
                AdditionalHeaders = new NameValueCollection();
            }

            AdditionalHeaders["Authorization"] = "Bearer " + string.Join(".", new []{ baseString, signatureString});
        }
    }
}