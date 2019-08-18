using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Gigya.Socialize.SDK.Internals;

namespace Gigya.Socialize.SDK
{
    public class GSAuthRequest : GSRequest
    {
        private readonly string _privateKey;

        /// <summary>
        /// Constructs a request using a userKey and privateKey
        /// Suitable for calling global sites REST API
        /// </summary>
        /// <param name="userKey">An administrative user's key.</param>
        /// <param name="privateKey">An administrative user's private key (PEM). usually read from file</param>
        /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
        /// If namespaces is not supplied "socialize" is assumed</param>
        /// <param name="clientParams">The request parameters</param>
        /// <param name="additionalHeaders">A collection of additional headers for the HTTP request.</param>
        /// <param name="proxy">Proxy for the HTTP request.</param>
        public GSAuthRequest(string userKey, string privateKey, string apiKey, string apiMethod, object clientParams = null,
            NameValueCollection additionalHeaders = null, IWebProxy proxy = null)
            : base(apiKey, null, apiMethod, clientParams, true, userKey, additionalHeaders, proxy)
        {
            if (userKey == null)
            {
                Logger.Write(new MissingFieldException("Authorized request must have userKey"));
                return;
            }
            _privateKey = privateKey;
        }

        protected override bool IsValidRequest()
        {
            return !string.IsNullOrEmpty(Method) && !string.IsNullOrEmpty(UserKey);
        }
        
        protected override void SetDefaultParams(string httpMethod, string resourceUri)
        {
            if (ApiKey != null)
                SetParam("apiKey", ApiKey);
        }

        protected override void Sign(string httpMethod, string resourceUri)
        {
            if (UserKey == null)
            {
                Logger.Write(new MissingFieldException("Failed to sign request, missing userKey"));
                return;
            }
            if (_privateKey == null)
            {
                Logger.Write(new MissingFieldException("Failed to sign request, missing privateKey"));
                return;
            }

            if (null == AdditionalHeaders)
            {
                AdditionalHeaders = new NameValueCollection();
            }

            AdditionalHeaders["Authorization"] = SigUtils.CalcAuthorizationBearer(UserKey, _privateKey);
        }
    }
}