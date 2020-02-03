/*
 * Copyright (C) 2011 Gigya, Inc.
 */

using System;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;


namespace Gigya.Socialize.SDK
{

    /// <summary>
    /// A Request to Gigya Socialize API 
    /// </summary>
    public class GSRequest
    {
        // Please don't change manually, will be auto increased by publishing script according to AssemblyInfo.
        public const String version = "2.16.4";

        /// <summary>
        /// This flag tells the SDK to try and reuse connections to the gigya servers, in order to lower the overheads
        /// associated with creating new connections, and to reduce latency. The drawback of long-lived connections is
        /// that they may become stale after a while, failing the next request sent on top of them. The SDK attempts to
        /// avoid that problem by purging old connections and re-issuing failed requests (just once). If, however, you
        /// encounter many failed requests to the Gigya API, you can try turning this feature off.
        /// </summary>
        public static bool EnableConnectionPooling = true;

        /// <summary>
        /// Set a proxy for the requests.
        /// </summary>
        public IWebProxy Proxy;

        /// <summary>
        /// The maximum number of concurrent connections that can remain open to the Gigya servers, assuming
        /// EnableConnectionPooling was enabled (above). Default: 100.
        /// </summary>
        public static int MaxConcurrentConnections
        {
            # region set/get
            get { return _maxConcurrentConnections; }
            set
            {
                if (_maxConcurrentConnections != value)
                {
                    _maxConcurrentConnections = value;
                    // Note: existing requests will release the previous semaphore (they cache it). Creating this new
                    // semaphore can cause new requests to be queued temporarily, until existing requests are completed.
                    _semaphore = new Semaphore(value, value);
                }
            }
            #endregion
        }

        /// <summary>The current number of concurrent connections that are open to the Gigya servers.</summary>
        public static int CurrentConnectionsUsed
        {
            get
            {
                if (_lastServicePointUsed != null)
                    return _lastServicePointUsed.CurrentConnections;
                else return 0;
            }
        }

        /// <summary>
        /// When all connections are in use (i.e. actively sending a request or receiving a response) and you attempt to
        /// send a new request, the SDK will perform one of two things:
        /// 
        /// If BlockWhenConnectionsExhausted is set to true, the SDK will block your thread until a connection frees up.
        /// Disadvantage: This entails using a semaphore, which adds a bit of overhead per request.
        /// 
        /// If BlockWhenConnectionsExhausted is set to false, .Net will queue your requests (at least as of .Net 3.5).
        /// Disadvantages:
        /// 1. Queued requests are stored in memory, and if you send requests significantly faster than the connections
        ///    pool is able to handle, you risk running out of memory eventually.
        /// 2. Queued requests are delayed significantly (could even reach hours), which may be unacceptable to you.
        /// 
        /// Default: true (block).
        /// </summary>
        public static bool BlockWhenConnectionsExhausted = true;

        /// <summary>
        /// The maximum length of JSon responses that this SDK is willing to accept from the server.
        /// </summary>
        public static uint MaxResponseSize = 50 * 1024 * 1024;

        /// <summary>
        /// Connections that are not in use for a while may expire and cause a network exception the next time a request
        /// is sent to Gigya. Instead of propagating the error, this flag tells the SDK to simply re-send the request
        /// (using another connection). If that connection expired too, the SDK will keep trying until all ACTIVE
        /// connections have been tried (which is usually significantly less than MaxConcurrentConnections).
        /// </summary>
        public bool RecoverFromExpiredConnections = true;

        /// <summary>
        /// When accessing different gigya data centers (e.g. eu.gigya.com), you may override the default "gigya.com"
        /// suffix using this member.
        /// </summary>
        public string APIDomain = "us1.gigya.com";

        /// <summary>(For Gigya internal use)</summary>
        public bool UseMethodDomain = true;

        #region Protected Members

        protected readonly string UserKey;
        protected readonly string ApiKey;
        protected readonly string Method;
        protected NameValueCollection AdditionalHeaders;
        protected readonly GSLogger Logger = new GSLogger();

        #endregion
        
        #region Private Members
        private string _domain;
        private string _path;
        private readonly string _secretKey;
        private readonly GSObject _dictionaryParams;
        private readonly bool _useHttps;
        private string _format;
        private const string UnreservedCharsString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        private static readonly char[] UnreservedChars;
        private GSResponse _immediateFailureResponse;
        private GSResponse _asyncResponse;
        private static ServicePoint _lastServicePointUsed;
        private static int _nonceCounter;
        private static int _maxConcurrentConnections = 100;
        private static Semaphore _semaphore = new Semaphore(_maxConcurrentConnections, _maxConcurrentConnections);

        /// <summary>
        /// THIS FIELD IS FOR TESTING PRUPOSES ONLY, setting this field to false involves a security risk.
        /// When set to false, requests won't be signed and the secret token will be sent instead.
        /// </summary>
        private bool SignRequests = true;

        /// <summary>
        /// The delta between our clock and Gigya's servers. We use it to correct the timestamp we put on outgoing
        /// signed requests (non-HTTPS). Otherwise if the drift between our clock and Gigya's servers' is too great
        /// (as of now over +-2 minutes), the requests will fail. We continuously adjust this value based on server
        /// responses timestamp.
        /// </summary>
        private static long timeCorrection = 0;


        IAsyncResult _asyncResult = null; // For asynchronous operation
        #endregion

        #region Contructors

        /// <summary>
        /// Static constructor
        /// </summary>
        static GSRequest()
        {
            UnreservedChars = UnreservedCharsString.ToCharArray();
            Array.Sort(UnreservedChars);
        }

        /// <summary>
        /// Constructs a request using an access token that was earlier obtained in the login proccess.
        /// The way to acuire the access token is beyond the scope of this class, but usualy it requires to 
        /// redirect the user to the login url, retrieve the "code" parameter and the exchange it for an access token (in some
        /// cases it is possible to get the access token in one step as a fragment parameter, e.g. www.example.com/callback#access_token=...)
        /// This kind of operation must be done over a secure connection (https).
        /// </summary>
        /// <param name="accessToken">the access token that was earlier obtained in the login proccess</param>        
        /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
        /// If namespaces is not supplied "socialize" is assumed</param>
        public GSRequest(string accessToken, string apiMethod)
            : this(accessToken, /*secretKey*/ null, apiMethod, /*clientParams*/ null, /*useHttps*/ true) { }

        /// <summary>
        /// Constructs a request using an access token that was earlier obtained in the login proccess.
        /// The way to acuire the access token is beyond the scope of this class, but usualy it requires to 
        /// redirect the user to the login url, retrieve the "code" parameter and the exchange it for an access token (in some
        /// cases it is possible to get the access token in one step as a fragment parameter, e.g. www.example.com/callback#access_token=...)
        /// This kind of operation must be done over a secure connection (https).
        /// </summary>
        /// <param name="accessToken">the access token that was earlier obtained in the login proccess</param>        
        /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
        /// If namespaces is not supplied "socialize" is assumed</param>
        /// <param name="clientParams">The request parameters</param>        
        public GSRequest(string accessToken, string apiMethod, object clientParams)
            : this(accessToken, /*secretKey*/ null, apiMethod, clientParams, /*useHttps*/ true) { }

        /// <summary>
        /// Constructs a request using an apiKey and secretKey.
        /// Suitable for calling our old REST API
        /// </summary>
        /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="secretKey">Secret Key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
        /// If namespaces is not supplied "socialize" is assumed</param>
        public GSRequest(string apiKey, string secretKey, string apiMethod)
            : this(apiKey, secretKey, apiMethod, /*clientParams*/ null, /*useHttps*/ false) { }

        /// <summary>
        /// Constructs a request using an apiKey and secretKey.
        /// Suitable for calling our old REST API
        /// </summary>
        /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="secretKey">Secret Key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
        /// If namespaces is not supplied "socialize" is assumed</param>        
        /// <param name="useHTTPS">Set this to true if you want to use HTTPS.
        /// The library uses HTTP by default (the request is signed with the secret key) 
        /// but you can use this parameter to override the default</param>
        public GSRequest(string apiKey, string secretKey, string apiMethod, bool useHTTPS)
            : this(apiKey, secretKey, apiMethod, /*clientParams*/ null, useHTTPS) { }

        /// <summary>
        /// Constructs a request using an apiKey and secretKey.
        /// Suitable for calling our old REST API
        /// </summary>
        /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="secretKey">Secret Key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
        /// If namespaces is not supplied "socialize" is assumed</param>
        /// <param name="clientParams">The request parameters</param>
        public GSRequest(string apiKey, string secretKey, string apiMethod, object clientParams)
            : this(apiKey, secretKey, apiMethod, clientParams, /*useHttps*/ false) { }

        /// <summary>
        /// Constructs a request using an apiKey and secretKey.
        /// Suitable for calling our old REST API
        /// </summary>
        /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="secretKey">Secret Key obtained from Site-Setup page on the Gigya website</param>
        /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
        /// If namespaces is not supplied "socialize" is assumed</param>
        /// <param name="clientParams">The request parameters</param>
        /// <param name="useHTTPS">Set this to true if you want to use HTTPS.
        /// The library uses HTTP by default (the request is signed with the secret key) 
        /// but you can use this parameter to override the default</param>
        /// <param name="userKey">An administrative user's key. If provided, the secretKey parameter is assumed to be
        /// that user's secret key and not the site's secret key. The apiKey may be null when calling permissions.* APIs
        /// which are not site-specific.</param>
        /// <param name="additionalHeaders">A collection of additional headers for the HTTP request.</param>
        /// <param name="proxy">Proxy for the HTTP request.</param>
        public GSRequest(string apiKey, string secretKey, string apiMethod, object clientParams, bool useHTTPS,
            string userKey = null, NameValueCollection additionalHeaders = null, IWebProxy proxy = null)
        {
            Proxy = proxy;
            GSObject gsObjParams = null;
            if (clientParams is GSObject)
                gsObjParams = clientParams as GSObject;
            else if (null != clientParams)
                gsObjParams = new GSObject(clientParams);

            if (string.IsNullOrEmpty(apiMethod))
                return;

            ApiKey = apiKey;
            UserKey = userKey;
            _secretKey = secretKey;
            Method = apiMethod;
            _useHttps = useHTTPS;
            AdditionalHeaders = additionalHeaders;
            _dictionaryParams = gsObjParams == null ? new GSObject() : gsObjParams.Clone();

            // Write to traceLog
            Logger.Write("apiMethod", apiMethod);
            if (secretKey != null)
            {
                if (apiKey != null)
                    Logger.Write("apiKey", apiKey);
                if (userKey != null)
                    Logger.Write("userKey", userKey);
            }
            else
            {
                Logger.Write("accessToken", apiKey);
            }

            Logger.Write("clientParams", _dictionaryParams.ToJsonString());
        }


        private void BuildUri(string apiMethod)
        {
            if (apiMethod.StartsWith("/"))
                apiMethod = apiMethod.Substring("/".Length);

            if (apiMethod.IndexOf(".", StringComparison.Ordinal) == -1)
            {
                _domain = UseMethodDomain ? "socialize." + this.APIDomain : this.APIDomain;
                _path = "/socialize." + apiMethod;
            }
            else
            {
                _domain = UseMethodDomain ? apiMethod.Split(new char[] { '\\', '.' })[0] + "." + this.APIDomain : this.APIDomain;
                _path = "/" + apiMethod;
            }
        }

        #endregion

        #region Public Methods
        #region - Set Params methods -
        /// <summary>
        ///  Associates the specified parameter with the specified value. 
        ///  If the parameter is already exists, the old value is replaced by the specified value.
        ///  If the parameter does not already exist, it will be inserted as a new parameter.
        /// </summary>
        /// <param name="param">parameter name to set</param>
        /// <param name="value">the value to set for the parameter</param>
        public void SetParam(string param, string value)
        {
            this._dictionaryParams.Put(param, value);
        }
        /// <summary>
        ///  Associates the specified parameter with the specified value. 
        ///  If the parameter is already exists, the old value is replaced by the specified value.
        ///  If the parameter does not already exist, it will be inserted as a new parameter.
        /// </summary>
        /// <param name="param">parameter name to set</param>
        /// <param name="value">the value to set for the parameter</param>
        public void SetParam(string param, long value)
        {
            this._dictionaryParams.Put(param, value);
        }
        /// <summary>
        ///  Associates the specified parameter with the specified value. 
        ///  If the parameter is already exists, the old value is replaced by the specified value.
        ///  If the parameter does not already exist, it will be inserted as a new parameter.
        /// </summary>
        /// <param name="param">parameter name to set</param>
        /// <param name="value">the value to set for the parameter</param>
        public void SetParam(string param, int value)
        {
            this._dictionaryParams.Put(param, value);
        }
        /// <summary>
        ///  Associates the specified parameter with the specified value. 
        ///  If the parameter is already exists, the old value is replaced by the specified value.
        ///  If the parameter does not already exist, it will be inserted as a new parameter.
        /// </summary>
        /// <param name="param">parameter name to set</param>
        /// <param name="value">the value to set for the parameter</param>
        public void SetParam(string param, bool value)
        {
            this._dictionaryParams.Put(param, value);
        }
        /// <summary>
        ///  Associates the specified parameter with the specified value. 
        ///  If the parameter is already exists, the old value is replaced by the specified value.
        ///  If the parameter does not already exist, it will be inserted as a new parameter.
        /// </summary>
        /// <param name="param">parameter name to set</param>
        /// <param name="value">the value to set for the parameter</param>
        public void SetParam(string param, GSObject value)
        {
            this._dictionaryParams.Put(param, value);
        }
        /// <summary>
        ///  Associates the specified parameter with the specified value. 
        ///  If the parameter is already exists, the old value is replaced by the specified value.
        ///  If the parameter does not already exist, it will be inserted as a new parameter.
        /// </summary>
        /// <param name="param">parameter name to set</param>
        /// <param name="value">the value to set for the parameter</param>
        public void SetParam(string param, GSArray value)
        {
            this._dictionaryParams.Put(param, value);
        }
        #endregion

        /// <summary>
        /// Returns a GSObject object containing the parameters of this request.
        /// </summary>
        /// <returns>the params field of this request.</returns>
        public GSObject GetParams()
        {
            return this._dictionaryParams;
        }

        /// <summary>
        /// Send the request synchronously
        /// </summary>
        /// <returns>a GSResponse object representing Gigya's response</returns>
        public GSResponse Send()
        {
            return this.Send(Timeout.Infinite);
        }

        /// <summary>
        /// Send the request synchronously
        /// </summary>
        /// <param name="timeout">Connection timeout in milliseconds</param>
        /// <returns>a GSResponse object representing Gigya's response</returns>
        public GSResponse Send(int timeout)
        {
            _format = _dictionaryParams.GetString("format", "json");
            
            if (!IsValidRequest())
            {
                return new GSResponse(Method, _dictionaryParams, 400002, Logger); // (Missing required parameter)
            }
            
            try
            {
                // Set default params
                SetParam("format", this._format);
                SetParam("httpStatusCodes", "false");

                BuildUri(Method);
                // Send request by HTTP POST
                var resp = SendRequestAndRecoverFromExpiredConnections("POST", timeout);

                // If we received an error from the server indicating that our clock isn't synchronized with the server's,
                // we adjust the time correction by the time delta and perform one retry.
                if (resp.GetErrorCode() == 403002 && resp.GetHeaders()["Date"] != null)
                {
                    timeCorrection = (long)(DateTime.Parse(resp.GetHeaders()["Date"]) - DateTime.Now).TotalMilliseconds;
                    resp = SendRequestAndRecoverFromExpiredConnections("POST", timeout);
                }

                return resp;
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.Timeout)
                    return new GSResponse(Method, _dictionaryParams, 504002, "Request Timeout", Logger);
                return new GSResponse(Method, _dictionaryParams, 500, e.ToString(), Logger);
            }
            catch (FormatException)
            {
                return new GSResponse(Method, _dictionaryParams, 400006, "Invalid parameter value: secret", Logger);
            }
            catch (Exception ex)
            {
                return new GSResponse(Method, _dictionaryParams, 500, ex.ToString(), Logger);
            }
        }


        /// <summary>
        /// Send the request asynchronously. You can pass an optional callback method to be called when the response
        /// arrives. The state parameter will be passed back to your callback function as IAsyncResult.AsyncState. If
        /// you do not pass a callback function (i.e. pass null), then you need to wait on the resulting
        /// IAsyncResult.AsyncWaitHandle. In either case, once you are signalled of the response, you need to call
        /// EndSend() to receive it.
        /// </summary>
        public IAsyncResult BeginSend(AsyncCallback callback, object state)
        {
            this._format = this._dictionaryParams.GetString("format", "json");

            if (!IsValidRequest())
            {
                // Missing required parameter
                return ImmediateAsyncFailure(400002, null, callback, state);
            }

            Semaphore sem = null;
            try
            {
                // Set default params
                this.SetParam("format", this._format);
                this.SetParam("httpStatusCodes", "false");
                int sendRetries = 0;

                BuildUri(this.Method);

                if (BlockWhenConnectionsExhausted)
                {
                    sem = _semaphore; // cache it in case settings change and it is modified
                    sem.WaitOne();
                }

                var reliableRequest = new GSAsyncReliableRequest(

                    // We pass a factory to generate requests
                    (out HttpWebRequest request_, out byte[] requestBody_) => { request_ = PrepareHttpRequest("POST", -1, out requestBody_); },

                    // Once we got a response we create and store the GSResponse object. If we encounter error #403002, we correct the timestamp and
                    // resend the request once.
                    asyncReq => ShouldResendRequest(asyncReq, ref sendRetries),

                    // We pass our own callback that releases the semaphore if needed before calling back the user.
                    ar =>
                    {
                        sem?.Release();
                        callback?.Invoke(ar);
                    },

                    state);

                _asyncResult = reliableRequest;
                reliableRequest.BeginReliableSend(); // Do this AFTER assigning _asyncResult; it may be re-entrant.
                return reliableRequest;
            }
            catch (FormatException)
            {
                sem?.Release();
                return ImmediateAsyncFailure(400006, "Invalid parameter value: secret", callback, state);
            }
            catch (Exception ex)
            {
                sem?.Release();
                return ImmediateAsyncFailure(500, EncodeJson(ex.ToString()), callback, state);
            }
        }

        protected virtual bool IsValidRequest()
        {
            return !(string.IsNullOrEmpty(Method)
                    || string.IsNullOrEmpty(ApiKey) && string.IsNullOrEmpty(UserKey)
                    || !string.IsNullOrEmpty(UserKey) && string.IsNullOrEmpty(_secretKey));
        }


        private bool ShouldResendRequest(GSAsyncRequest asyncReq, ref int sendRetries)
        {
            if (asyncReq.Error != null)
                if (asyncReq.Error is WebException
                    && (asyncReq.Error as WebException).Status == WebExceptionStatus.KeepAliveFailure
                    && RecoverFromExpiredConnections
                    && sendRetries++ < asyncReq.Request.ServicePoint.CurrentConnections)
                    return true;
                else _asyncResponse = new GSResponse(this.Method, _dictionaryParams, 500, EncodeJson(asyncReq.Error.ToString()), Logger);
            else
            {
                Logger.Write("server", asyncReq.Response.Headers["x-server"]);
                _asyncResponse = new GSResponse(this.Method, ExtractHeaders(asyncReq.Response.Headers), asyncReq.ResponseBody, Logger);
            }
            if (_asyncResponse.GetErrorCode() == 403002 && _asyncResponse.GetHeaders()["Date"] != null)
            {
                timeCorrection = (long)(DateTime.Parse(_asyncResponse.GetHeaders()["Date"]) - DateTime.Now).TotalMilliseconds;
                if (sendRetries++ == 0)
                    return true;
            }
            return false;
        }


        IAsyncResult ImmediateAsyncFailure(int errorCode, string errorMessage, AsyncCallback callback, object state)
        {
            _immediateFailureResponse = new GSResponse(Method, _dictionaryParams, errorCode, errorMessage, Logger);
            _asyncResult = new GSAsync(state, new ManualResetEvent(true), true, true);
            callback?.Invoke(_asyncResult);
            return _asyncResult;
        }


        Dictionary<string, string> ExtractHeaders(WebHeaderCollection headers)
        {
            var d = new Dictionary<string, string>();
            for (int i = 0; i < headers.Count; ++i)
                d.Add(headers.AllKeys[i], headers.Get(i));
            return d;
        }


        public GSResponse EndSend(IAsyncResult iar)
        {
            if (iar == null) throw new ArgumentNullException("iar");
            if (_asyncResult == null) throw new InvalidOperationException("Cannot call EndSend before calling BeginSend");
            if (_asyncResult != iar) throw new ArgumentException("The IAsyncResult object was not returned from the corresponding asynchronous method on this class.", "iar");

            // If an invalid call to BeginSend() was made, we previously stored a locally-generated reply with error
            // details and now return it to the user (as if it was a real reply from the server).
            if (_immediateFailureResponse != null)
                return _immediateFailureResponse;

            else return _asyncResponse;
        }

        /// <summary>
        /// Aborts an asynchronous request that you previously initiated using BeginSend().
        /// </summary>
        /// <param name="iar"></param>
        public void Abort()
        {
            if (_asyncResult == null) throw new InvalidOperationException("Cannot call Abort before calling BeginSend");
            if (_immediateFailureResponse == null)
                ((GSAsyncReliableRequest)_asyncResult).GSAsyncRequest.Request.Abort();
        }

        /// <summary>
        ///  Converts a GSObject to a query string
        /// </summary>
        /// <param name="addQuestionMark">Set to true if you want the returned string to start with a question mark.</param>
        /// <param name="paramDictionary">the GSObject to get the query string from</param>
        /// <returns></returns>
        public static string BuildQS(bool addQuestionMark, GSObject paramDictionary)
        {
            StringBuilder retQS = new StringBuilder();
            string value;
            if (addQuestionMark)
                retQS.Append("?");

            foreach (string key in paramDictionary.GetKeys())
            {
                value = paramDictionary.GetString(key, null);
                if (value != null)
                {
                    retQS.Append(key);
                    retQS.Append('=');
                    retQS.Append(GSRequest.UrlEncode(value));
                    retQS.Append('&');
                }
            }

            // Remove the '&' at the end by trimming the end of the StringBuilder
            if (retQS[retQS.Length - 1] == '&')
                retQS.Length--;

            return retQS.ToString();
        }

        /// <summary>
        /// Applies URL encoding rules to the String value, and returns the outcome.
        /// </summary>
        /// <param name="value">the string to encode</param>
        /// <returns>the URL encoded string</returns>
        public static string UrlEncode(string value)
        {
            StringBuilder result = new StringBuilder();
            char[] c = new char[1];

            for (int i = 0; i < value.Length; i++)
            {
                char symbol = value[i];
                if (Array.BinarySearch<char>(UnreservedChars, symbol) >= 0)
                    result.Append(symbol);
                else
                {
                    c[0] = symbol;
                    byte[] bytes = Encoding.UTF8.GetBytes(c);
                    foreach (byte b in bytes)
                        result.Append('%' + String.Format("{0:X2}", (int)b));
                }
            }

            return result.ToString();
        }
        #endregion


        #region Private Methods

        private HttpWebRequest PrepareHttpRequest(string httpMethod, int timeout, out byte[] requestBody)
        {
            // Set Protocol and URI
            var protocol =
                _useHttps || _secretKey == null || !SignRequests ? "https" : "http";
            var resourceUri = protocol + "://" + _domain + _path;

            SetDefaultParams(httpMethod, resourceUri);
            
            SetParam("sdk", "dotnet_" + version);
            
            Sign(httpMethod, resourceUri);

            Logger.Write("serverParams", _dictionaryParams);
            // Build the query string from the dictionary
            var data = BuildQS(false /*dont add question mark*/, _dictionaryParams);

            Logger.Write("URL", resourceUri);
            Logger.Write("postData", data);

            // Create HttpRequest
            var request = (HttpWebRequest)WebRequest.Create(resourceUri);
            request.Timeout = timeout;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = httpMethod;
            request.KeepAlive = EnableConnectionPooling;
            if (EnableConnectionPooling)
            {
                request.Headers["X-Connection"] = "Keep-Alive";
                request.ServicePoint.ConnectionLimit = _maxConcurrentConnections;
                request.ServicePoint.MaxIdleTime = 180000;
            }
            _lastServicePointUsed = request.ServicePoint;
            requestBody = Encoding.UTF8.GetBytes(data);
            request.ContentLength = requestBody.Length;
            if (null != AdditionalHeaders)
                request.Headers.Add(AdditionalHeaders);
            request.ServicePoint.Expect100Continue = false;
            return request;
        }

        protected virtual void SetDefaultParams(string httpMethod, string resourceUri)
        {
            if (_secretKey == null)
            {
                SetParam("oauth_token", ApiKey);
            }
            else // Use apiKey and sign the request using the secretKey
            {
                if (ApiKey != null)
                    SetParam("apiKey", ApiKey);
                if (UserKey != null)
                    SetParam("userKey", UserKey);
            }
        }

        protected virtual void Sign(string httpMethod, string resourceUri)
        {
            // Set Timestamp and Nonce
            var currTime = SigUtils.CurrentTimeMillis() + timeCorrection;
            var nonce = DateTime.Now.ToFileTime() + "_" + Interlocked.Increment(ref _nonceCounter);

            // Set params. DO THIS *BEFORE* CALCULATING THE SIGNATURE 

            if (SignRequests)//Parameter needed only for signature.
            {
                var timestamp = (currTime / 1000).ToString();
                SetParam("timestamp", timestamp);
                SetParam("nonce", nonce);
            }

            // Calculate signature. DO THIS ONLY *AFTER* PUTTING ALL OTHER PARAMS IN DICTIONARY
            _dictionaryParams.Remove("sig"); // In case we attempted to send that request already
            var baseString = SigUtils.CalcOAuth1Basestring(httpMethod, resourceUri, _dictionaryParams);
            Logger.Write("baseString", baseString);

            if (SignRequests)
            {
                var signature = SigUtils.CalcSignature(baseString, _secretKey);
                SetParam("sig", signature);
            }
            else //Send secret key explicitly.
            {
                SetParam("secret", _secretKey);
            }
        }


        /// <summary>
        /// This method keeps re-sending the request as long as we encounter an error from an expired connection, up to
        /// the number of active connections, unless RecoverFromExpiredConnections is set to false.
        /// </summary>
        GSResponse SendRequestAndRecoverFromExpiredConnections(string httpMethod, int timeout)
        {
            int retries = 0;
            while (true)
            {
                HttpWebRequest request = null;
                try
                {
                    return SendRequest(httpMethod, timeout, out request);
                }
                catch (WebException e)
                {
                    if (e.Status != WebExceptionStatus.KeepAliveFailure
                        || !RecoverFromExpiredConnections
                        || request == null
                        || retries++ > request.ServicePoint.CurrentConnections)
                        throw;
                }
            }
        }


        /// <summary>
        /// Send the actual HTTP/S request
        /// </summary>
        /// <param name="httpMethod">"POST" or "GET"</param>        
        /// <returns></returns>
        private GSResponse SendRequest(string httpMethod, int timeout, out HttpWebRequest request)
        {
            byte[] form_body;
            request = PrepareHttpRequest(httpMethod, timeout, out form_body);
            if (Proxy != null) request.Proxy = Proxy;

            // Write content to the request.
            using (var stream = request.GetRequestStream())
            {
                stream.Write(form_body, 0, form_body.Length);
                stream.Close();
            }

            // Get HttpResponse
            using (var webResponse = (HttpWebResponse)request.GetResponse())
            {
                Logger.Write("server", webResponse.Headers["x-server"]);
                using (StreamReader sr = new StreamReader(webResponse.GetResponseStream(), Encoding.GetEncoding(webResponse.CharacterSet ?? "utf-8")))
                    return new GSResponse(Method, ExtractHeaders(webResponse.Headers), sr.ReadToEnd(), Logger);
            }
        }


        // Note: Once we upgrade to .Net 4, use HttpUtility.JavaScriptStringEncode() instead.
        static string EncodeJson(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        int i = (int)c;
                        if (i < 32 || i > 127)
                            sb.AppendFormat("\\u{0:X04}", i);
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }


        #endregion

    }
}