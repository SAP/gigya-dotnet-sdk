/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GSRequest
 * Uses HttpClient exclusively (no legacy HttpWebRequest)
 */

using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Gigya.Socialize.SDK;

/// <summary>
/// A Request to Gigya Socialize API.
/// Uses modern HttpClient for all HTTP operations.
/// </summary>
public class GSRequest
{
    // Please don't change manually, will be auto increased by publishing script according to AssemblyInfo.
    /// <summary>
    /// SDK version string.
    /// </summary>
    public const string Version = "3.0.0";

    #region Static Configuration

    private static bool _enableConnectionPooling = true;
    private static int _maxConcurrentConnections = 100;
    private static long _timeCorrection = 0;
    private static int _nonceCounter = 0;
    private static readonly object _configLock = new();
    private static bool _configurationLocked;
    private static Lazy<HttpClient> _lazyHttpClient = CreateLazyHttpClient();

    /// <summary>
    /// This flag tells the SDK to try and reuse connections to the gigya servers, in order to lower the overheads
    /// associated with creating new connections, and to reduce latency. The drawback of long-lived connections is
    /// that they may become stale after a while, failing the next request sent on top of them. The SDK attempts to
    /// avoid that problem by purging old connections and re-issuing failed requests (just once). If, however, you
    /// encounter many failed requests to the Gigya API, you can try turning this feature off.
    /// Must be set before the first request is sent.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if attempting to change after HttpClient has been initialized.</exception>
    public static bool EnableConnectionPooling
    {
        get => _enableConnectionPooling;
        set
        {
            if (_enableConnectionPooling != value)
            {
                lock (_configLock)
                {
                    if (_configurationLocked)
                    {
                        throw new InvalidOperationException(
                            "Cannot change EnableConnectionPooling after the first request has been sent. " +
                            "Configure this value at application startup before making any requests.");
                    }
                    _enableConnectionPooling = value;
                }
            }
        }
    }

    /// <summary>
    /// The maximum length of JSON responses that this SDK is willing to accept from the server.
    /// Default: 50MB.
    /// </summary>
    public static uint MaxResponseSize = 50 * 1024 * 1024;

    #endregion

    #region Instance Configuration

    /// <summary>
    /// Set a proxy for the requests.
    /// </summary>
    public IWebProxy? Proxy { get; set; }

    /// <summary>
    /// Connections that are not in use for a while may expire and cause a network exception the next time a request
    /// is sent to Gigya. Instead of propagating the error, this flag tells the SDK to simply re-send the request
    /// (using another connection). If that connection expired too, the SDK will keep trying until all ACTIVE
    /// connections have been tried (which is usually significantly less than MaxConcurrentConnections).
    /// Default: true.
    /// </summary>
    public bool RecoverFromExpiredConnections { get; set; } = true;

    /// <summary>
    /// When accessing different gigya data centers (e.g. eu1.gigya.com), you may override the default "us1.gigya.com"
    /// suffix using this member.
    /// </summary>
    public string APIDomain { get; set; } = "us1.gigya.com";

    /// <summary>
    /// (For Gigya internal use)
    /// </summary>
    public bool UseMethodDomain { get; set; } = true;

    /// <summary>
    /// Domain override for the request (used by mTLS).
    /// </summary>
    public string? DomainForOverride { get; set; }

    #endregion

    #region Protected Members

    /// <summary>
    /// The user key for authentication.
    /// </summary>
    protected readonly string? UserKey;

    /// <summary>
    /// The API key.
    /// </summary>
    protected readonly string? ApiKey;

    /// <summary>
    /// The API method to call.
    /// </summary>
    protected readonly string? Method;

    /// <summary>
    /// Additional HTTP headers.
    /// </summary>
    protected NameValueCollection? AdditionalHeaders;

    /// <summary>
    /// Logger for diagnostic information.
    /// </summary>
    protected readonly GSLogger Logger = new();

    #endregion

    #region Private Members

    private string? _domain;
    private string? _path;
    private readonly string? _secretKey;
    private readonly GSObject _dictionaryParams = new();
    private readonly bool _useHttps;
    private string _format = "json";
    private bool _signRequests = true;

    private static readonly char[] UnreservedChars;
    private const string UnreservedCharsString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

    #endregion

    /// <summary>
    /// The maximum number of concurrent connections that can remain open to the Gigya servers, assuming
    /// EnableConnectionPooling was enabled (above). Default: 100.
    /// Must be set before the first request is sent.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if attempting to change after HttpClient has been initialized.</exception>
    public static int MaxConcurrentConnections
    {
        get => _maxConcurrentConnections;
        set
        {
            if (_maxConcurrentConnections != value)
            {
                lock (_configLock)
                {
                    if (_configurationLocked)
                    {
                        throw new InvalidOperationException(
                            "Cannot change MaxConcurrentConnections after the first request has been sent. " +
                            "Configure this value at application startup before making any requests.");
                    }
                    _maxConcurrentConnections = value;
                }
            }
        }
    }

    #region Constructors

    /// <summary>
    /// Static constructor
    /// </summary>
    static GSRequest()
    {
        UnreservedChars = UnreservedCharsString.ToCharArray();
        Array.Sort(UnreservedChars);
    }

    /// <summary>
    /// Constructs a request using an access token that was earlier obtained in the login process.
    /// The way to acquire the access token is beyond the scope of this class, but usually it requires to 
    /// redirect the user to the login url, retrieve the "code" parameter and then exchange it for an access token (in some
    /// cases it is possible to get the access token in one step as a fragment parameter, e.g. www.example.com/callback#access_token=...)
    /// This kind of operation must be done over a secure connection (https).
    /// </summary>
    /// <param name="accessToken">The access token that was earlier obtained in the login process.</param>        
    /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
    /// If namespace is not supplied "socialize" is assumed.</param>
    public GSRequest(string accessToken, string apiMethod)
        : this(accessToken, null, apiMethod, null, true)
    {
    }

    /// <summary>
    /// Constructs a request using an access token that was earlier obtained in the login process.
    /// The way to acquire the access token is beyond the scope of this class, but usually it requires to 
    /// redirect the user to the login url, retrieve the "code" parameter and then exchange it for an access token (in some
    /// cases it is possible to get the access token in one step as a fragment parameter, e.g. www.example.com/callback#access_token=...)
    /// This kind of operation must be done over a secure connection (https).
    /// </summary>
    /// <param name="accessToken">The access token that was earlier obtained in the login process.</param>        
    /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
    /// If namespace is not supplied "socialize" is assumed.</param>
    /// <param name="clientParams">The request parameters.</param>        
    public GSRequest(string accessToken, string apiMethod, GSObject? clientParams)
        : this(accessToken, null, apiMethod, clientParams, true)
    {
    }

    /// <summary>
    /// Constructs a request using an apiKey and secretKey.
    /// Suitable for calling our old REST API.
    /// </summary>
    /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website.</param>
    /// <param name="secretKey">Secret Key obtained from Site-Setup page on the Gigya website.</param>
    /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
    /// If namespace is not supplied "socialize" is assumed.</param>
    public GSRequest(string apiKey, string? secretKey, string apiMethod)
        : this(apiKey, secretKey, apiMethod, null, false)
    {
    }

    /// <summary>
    /// Constructs a request using an apiKey and secretKey.
    /// Suitable for calling our old REST API.
    /// </summary>
    /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website.</param>
    /// <param name="secretKey">Secret Key obtained from Site-Setup page on the Gigya website.</param>
    /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
    /// If namespace is not supplied "socialize" is assumed.</param>        
    /// <param name="useHttps">Set this to true if you want to use HTTPS.
    /// The library uses HTTP by default (the request is signed with the secret key) 
    /// but you can use this parameter to override the default.</param>
    public GSRequest(string apiKey, string? secretKey, string apiMethod, bool useHttps)
        : this(apiKey, secretKey, apiMethod, null, useHttps)
    {
    }

    /// <summary>
    /// Constructs a request using an apiKey and secretKey.
    /// Suitable for calling our old REST API.
    /// </summary>
    /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website.</param>
    /// <param name="secretKey">Secret Key obtained from Site-Setup page on the Gigya website.</param>
    /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
    /// If namespace is not supplied "socialize" is assumed.</param>
    /// <param name="clientParams">The request parameters.</param>
    public GSRequest(string apiKey, string? secretKey, string apiMethod, object? clientParams)
        : this(apiKey, secretKey, apiMethod, clientParams, false)
    {
    }

    /// <summary>
    /// Constructs a request using an apiKey and secretKey.
    /// Suitable for calling our old REST API.
    /// </summary>
    /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website.</param>
    /// <param name="secretKey">Secret Key obtained from Site-Setup page on the Gigya website.</param>
    /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
    /// If namespace is not supplied "socialize" is assumed.</param>
    /// <param name="clientParams">The request parameters.</param>
    /// <param name="useHttps">Set this to true if you want to use HTTPS.
    /// The library uses HTTP by default (the request is signed with the secret key) 
    /// but you can use this parameter to override the default.</param>
    /// <param name="userKey">An administrative user's key. If provided, the secretKey parameter is assumed to be
    /// that user's secret key and not the site's secret key. The apiKey may be null when calling permissions.* APIs
    /// which are not site-specific.</param>
    /// <param name="additionalHeaders">A collection of additional headers for the HTTP request.</param>
    /// <param name="proxy">Proxy for the HTTP request.</param>
    public GSRequest(
        string? apiKey,
        string? secretKey,
        string apiMethod,
        object? clientParams,
        bool useHttps,
        string? userKey = null,
        NameValueCollection? additionalHeaders = null,
        IWebProxy? proxy = null)
    {
        Proxy = proxy;
        
        GSObject? gsObjParams = clientParams switch
        {
            GSObject gsObj => gsObj,
            not null => new GSObject(clientParams),
            _ => null
        };

        if (string.IsNullOrEmpty(apiMethod))
            return;

        ApiKey = apiKey;
        UserKey = userKey;
        _secretKey = secretKey;
        Method = apiMethod;
        _useHttps = useHttps;
        AdditionalHeaders = additionalHeaders;
        _dictionaryParams = gsObjParams?.Clone() ?? new GSObject();

        // Write to trace log
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

    #endregion

    #region Parameter Methods

    /// <summary>
    /// Associates the specified parameter with the specified value. 
    /// If the parameter already exists, the old value is replaced by the specified value.
    /// If the parameter does not already exist, it will be inserted as a new parameter.
    /// </summary>
    /// <param name="param">Parameter name to set.</param>
    /// <param name="value">The value to set for the parameter.</param>
    public void SetParam(string param, string value) => _dictionaryParams.Put(param, value);

    /// <summary>
    /// Associates the specified parameter with the specified value. 
    /// If the parameter already exists, the old value is replaced by the specified value.
    /// If the parameter does not already exist, it will be inserted as a new parameter.
    /// </summary>
    /// <param name="param">Parameter name to set.</param>
    /// <param name="value">The value to set for the parameter.</param>
    public void SetParam(string param, long value) => _dictionaryParams.Put(param, value);

    /// <summary>
    /// Associates the specified parameter with the specified value. 
    /// If the parameter already exists, the old value is replaced by the specified value.
    /// If the parameter does not already exist, it will be inserted as a new parameter.
    /// </summary>
    /// <param name="param">Parameter name to set.</param>
    /// <param name="value">The value to set for the parameter.</param>
    public void SetParam(string param, int value) => _dictionaryParams.Put(param, value);

    /// <summary>
    /// Associates the specified parameter with the specified value. 
    /// If the parameter already exists, the old value is replaced by the specified value.
    /// If the parameter does not already exist, it will be inserted as a new parameter.
    /// </summary>
    /// <param name="param">Parameter name to set.</param>
    /// <param name="value">The value to set for the parameter.</param>
    public void SetParam(string param, bool value) => _dictionaryParams.Put(param, value);

    /// <summary>
    /// Associates the specified parameter with the specified value. 
    /// If the parameter already exists, the old value is replaced by the specified value.
    /// If the parameter does not already exist, it will be inserted as a new parameter.
    /// </summary>
    /// <param name="param">Parameter name to set.</param>
    /// <param name="value">The value to set for the parameter.</param>
    public void SetParam(string param, GSObject value) => _dictionaryParams.Put(param, value);

    /// <summary>
    /// Associates the specified parameter with the specified value. 
    /// If the parameter already exists, the old value is replaced by the specified value.
    /// If the parameter does not already exist, it will be inserted as a new parameter.
    /// </summary>
    /// <param name="param">Parameter name to set.</param>
    /// <param name="value">The value to set for the parameter.</param>
    public void SetParam(string param, GSArray value) => _dictionaryParams.Put(param, value);

    /// <summary>
    /// Returns a GSObject object containing the parameters of this request.
    /// </summary>
    /// <returns>The params field of this request.</returns>
    public GSObject GetParams() => _dictionaryParams;

    #endregion

    #region Send Methods

    /// <summary>
    /// Send the request synchronously.
    /// </summary>
    /// <returns>A GSResponse object representing Gigya's response.</returns>
    public GSResponse Send() => Send(Timeout.Infinite);

    /// <summary>
    /// Send the request synchronously.
    /// </summary>
    /// <param name="timeout">Connection timeout in milliseconds. Use Timeout.Infinite for no timeout.</param>
    /// <returns>A GSResponse object representing Gigya's response.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if timeout is negative (except Timeout.Infinite).</exception>
    public GSResponse Send(int timeout)
    {
        if (timeout < 0 && timeout != Timeout.Infinite)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be non-negative or Timeout.Infinite");
        
        var timeoutSpan = timeout == Timeout.Infinite 
            ? System.Threading.Timeout.InfiniteTimeSpan 
            : TimeSpan.FromMilliseconds(timeout);
        
        // Use async implementation synchronously
        return SendAsync(timeoutSpan, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Sends the request asynchronously using modern HttpClient.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the server.</returns>
    public virtual async Task<GSResponse> SendAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync(System.Threading.Timeout.InfiniteTimeSpan, cancellationToken);
    }

    /// <summary>
    /// Sends the request asynchronously with specified timeout using modern HttpClient.
    /// </summary>
    /// <param name="timeout">Request timeout. Use TimeSpan.Zero or Timeout.InfiniteTimeSpan for no timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the server.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if timeout is negative (except InfiniteTimeSpan).</exception>
    public virtual async Task<GSResponse> SendAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (timeout < TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be non-negative or Timeout.InfiniteTimeSpan");
        
        _format = _dictionaryParams.GetString("format", "json") ?? "json";

        if (!IsValidRequest())
        {
            return new GSResponse(Method!, _dictionaryParams, 400002, Logger);
        }

        try
        {
            SetParam("format", _format);
            SetParam("httpStatusCodes", "false");

            BuildUri(Method!);

            var response = await SendRequestWithRetryAsync(timeout, cancellationToken);

            // Handle clock synchronization error
            if (response.GetErrorCode() == 403002)
            {
                var headers = response.GetHeaders();
                if (headers.TryGetValue("Date", out var dateStr))
                {
                    // Use RFC1123 format for HTTP dates with invariant culture
                    if (DateTime.TryParseExact(dateStr, "R", CultureInfo.InvariantCulture, DateTimeStyles.None, out var serverTime))
                    {
                        Interlocked.Exchange(ref _timeCorrection, (long)(serverTime - DateTime.Now).TotalMilliseconds);
                        response = await SendRequestWithRetryAsync(timeout, cancellationToken);
                    }
                }
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Re-throw cancellation
        }
        catch (TimeoutException)
        {
            return new GSResponse(Method!, _dictionaryParams, 504002, "Request Timeout", Logger);
        }
        catch (HttpRequestException ex)
        {
            return new GSResponse(Method!, _dictionaryParams, 500, ex.ToString(), Logger);
        }
        catch (FormatException)
        {
            return new GSResponse(Method!, _dictionaryParams, 400006, "Invalid parameter value: secret", Logger);
        }
        catch (Exception ex)
        {
            return new GSResponse(Method!, _dictionaryParams, 500, ex.ToString(), Logger);
        }
    }

    #endregion

    #region Legacy Async Methods (APM Pattern)

    /// <summary>
    /// Send the request asynchronously. You can pass an optional callback method to be called when the response
    /// arrives. The state parameter will be passed back to your callback function as IAsyncResult.AsyncState. If
    /// you do not pass a callback function (i.e. pass null), then you need to wait on the resulting
    /// IAsyncResult.AsyncWaitHandle. In either case, once you are signalled of the response, you need to call
    /// EndSend() to receive it.
    /// </summary>
    /// <param name="callback">Optional callback method to be called when the response arrives.</param>
    /// <param name="state">State parameter that will be passed back to your callback function.</param>
    /// <returns>An IAsyncResult that can be used to wait for the response.</returns>
    [Obsolete("Use SendAsync instead.")]
    public IAsyncResult BeginSend(AsyncCallback? callback, object? state)
    {
        var task = SendAsync();
        return new TaskAsyncResult(task, callback, state);
    }

    /// <summary>
    /// Ends an asynchronous send operation.
    /// </summary>
    [Obsolete("Use SendAsync instead.")]
    public GSResponse EndSend(IAsyncResult asyncResult)
    {
        if (asyncResult is TaskAsyncResult tar)
        {
            tar.Task.Wait();
            return tar.Task.Result;
        }
        throw new ArgumentException("Invalid IAsyncResult", nameof(asyncResult));
    }

    /// <summary>
    /// Aborts an asynchronous request that you previously initiated using BeginSend().
    /// </summary>
    [Obsolete("Use CancellationToken with SendAsync instead.")]
    public void Abort()
    {
        // No-op in modern implementation
    }

    #endregion

    #region Static Utility Methods

    /// <summary>
    /// Converts a GSObject to a query string.
    /// </summary>
    /// <param name="addQuestionMark">Set to true if you want the returned string to start with a question mark.</param>
    /// <param name="paramDictionary">The GSObject to get the query string from.</param>
    /// <returns>The query string representation of the GSObject.</returns>
    public static string BuildQS(bool addQuestionMark, GSObject paramDictionary)
    {
        var sb = new StringBuilder();
        if (addQuestionMark)
            sb.Append('?');

        foreach (var key in paramDictionary.GetKeys())
        {
            var value = paramDictionary.GetString(key, null);
            if (value != null)
            {
                sb.Append(key);
                sb.Append('=');
                sb.Append(UrlEncode(value));
                sb.Append('&');
            }
        }

        if (sb.Length > 0 && sb[^1] == '&')
            sb.Length--;

        return sb.ToString();
    }

    /// <summary>
    /// Applies URL encoding rules to the String value, and returns the outcome.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <returns>The URL encoded string.</returns>
    public static string UrlEncode(string value)
    {
        var result = new StringBuilder();

        foreach (var symbol in value)
        {
            if (Array.BinarySearch(UnreservedChars, symbol) >= 0)
            {
                result.Append(symbol);
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(new[] { symbol });
                foreach (var b in bytes)
                    result.Append('%' + b.ToString("X2"));
            }
        }

        return result.ToString();
    }

    #endregion

    #region Protected Virtual Methods

    /// <summary>
    /// Validates the request before sending.
    /// </summary>
    protected virtual bool IsValidRequest()
    {
        return !string.IsNullOrEmpty(Method)
               && (!string.IsNullOrEmpty(ApiKey) || !string.IsNullOrEmpty(UserKey))
               && (string.IsNullOrEmpty(UserKey) || !string.IsNullOrEmpty(_secretKey));
    }

    /// <summary>
    /// Gets the domain for the request. Can be overridden by subclasses to use a different domain.
    /// </summary>
    /// <param name="methodNamespace">The namespace of the API method (e.g., "accounts", "socialize").</param>
    /// <returns>The domain to use for this request.</returns>
    protected virtual string GetRequestDomain(string methodNamespace)
    {
        return UseMethodDomain ? methodNamespace + "." + APIDomain : APIDomain;
    }

    /// <summary>
    /// Sets default parameters before signing.
    /// </summary>
    protected virtual void SetDefaultParams(string httpMethod, string resourceUri)
    {
        if (_secretKey == null)
        {
            SetParam("oauth_token", ApiKey!);
        }
        else
        {
            if (ApiKey != null)
                SetParam("apiKey", ApiKey);
            if (UserKey != null)
                SetParam("userKey", UserKey);
        }
    }

    /// <summary>
    /// Signs the request.
    /// </summary>
    protected virtual void Sign(string httpMethod, string resourceUri)
    {
        var currTime = SigUtils.CurrentTimeMillis() + Interlocked.Read(ref _timeCorrection);
        var nonce = DateTime.Now.ToFileTime() + "_" + Interlocked.Increment(ref _nonceCounter);

        if (_signRequests)
        {
            var timestamp = (currTime / 1000).ToString();
            SetParam("timestamp", timestamp);
            SetParam("nonce", nonce);
        }

        _dictionaryParams.Remove("sig");
        var baseString = SigUtils.CalcOAuth1Basestring(httpMethod, resourceUri, _dictionaryParams);
        Logger.Write("baseString", baseString);

        if (_signRequests)
        {
            var signature = SigUtils.CalcSignature(baseString, _secretKey!);
            SetParam("sig", signature);
        }
        else
        {
            SetParam("secret", _secretKey!);
        }
    }

    /// <summary>
    /// Creates the HttpClient for this request.
    /// Override in derived classes to customize (e.g., to add client certificates for mTLS).
    /// </summary>
    /// <returns>The HttpClient to use for this request.</returns>
    protected virtual HttpClient GetHttpClient()
    {
        return GetSharedHttpClient();
    }

    /// <summary>
    /// Virtual method hook to allow derived classes to configure the SocketsHttpHandler
    /// before it is used (e.g., to add client certificates for mTLS).
    /// </summary>
    /// <param name="handler">The SocketsHttpHandler to configure.</param>
    protected virtual void ConfigureHandler(SocketsHttpHandler handler)
    {
        // Base implementation - no additional configuration
    }

    #endregion

    #region Private Methods

    private void BuildUri(string apiMethod)
    {
        if (apiMethod.StartsWith("/"))
            apiMethod = apiMethod[1..];

        if (!apiMethod.Contains('.'))
        {
            _domain = GetRequestDomain("socialize");
            _path = "/socialize." + apiMethod;
        }
        else
        {
            var methodNamespace = apiMethod.Split('.', '\\')[0];
            _domain = GetRequestDomain(methodNamespace);
            _path = "/" + apiMethod;
        }
    }

    /// <summary>
    /// Creates a Lazy&lt;HttpClient&gt; with thread-safe initialization.
    /// </summary>
    private static Lazy<HttpClient> CreateLazyHttpClient()
    {
        return new Lazy<HttpClient>(() =>
        {
            // Lock configuration once HttpClient is being created
            lock (_configLock)
            {
                _configurationLocked = true;
            }

            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                MaxConnectionsPerServer = _maxConcurrentConnections,
                PooledConnectionLifetime = _enableConnectionPooling ? TimeSpan.FromMinutes(5) : TimeSpan.Zero,
                PooledConnectionIdleTimeout = _enableConnectionPooling ? TimeSpan.FromMinutes(2) : TimeSpan.Zero,
                EnableMultipleHttp2Connections = true
            };

            return new HttpClient(handler)
            {
                Timeout = System.Threading.Timeout.InfiniteTimeSpan
            };
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private static HttpClient GetSharedHttpClient()
    {
        return _lazyHttpClient.Value;
    }

    private static void ResetSharedHttpClient()
    {
        lock (_configLock)
        {
            if (_lazyHttpClient.IsValueCreated)
            {
                _lazyHttpClient.Value.Dispose();
            }
            _lazyHttpClient = CreateLazyHttpClient();
            _configurationLocked = false;
        }
    }

    private async Task<GSResponse> SendRequestWithRetryAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                return await SendRequestAsync(timeout, cancellationToken);
            }
            catch (HttpRequestException ex) when (IsRetryableException(ex) && RecoverFromExpiredConnections && retries++ <= 5)
            {
                // Retry on connection failures
                await Task.Delay(100 * retries, cancellationToken);
            }
        }
    }

    private static bool IsRetryableException(HttpRequestException ex)
    {
        // Retry on connection reset, connection refused, etc.
        return ex.InnerException is System.IO.IOException ||
               ex.InnerException is System.Net.Sockets.SocketException;
    }

    /// <summary>
    /// Sends the request asynchronously using HttpClient.
    /// </summary>
    protected virtual async Task<GSResponse> SendRequestAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var protocol = _useHttps || _secretKey == null || !_signRequests ? "https" : "http";
        var resourceUri = protocol + "://" + _domain + _path;

        SetDefaultParams("POST", resourceUri);
        SetParam("sdk", "dotnet_" + Version);
        Sign("POST", resourceUri);

        Logger.Write("serverParams", _dictionaryParams);

        // Build form content
        var formContent = new Dictionary<string, string>();
        foreach (var key in _dictionaryParams.GetKeys())
        {
            var value = _dictionaryParams.GetString(key, null);
            if (value != null)
            {
                formContent[key] = value;
            }
        }

        Logger.Write("URL", resourceUri);

        // Create request
        using var request = new HttpRequestMessage(HttpMethod.Post, resourceUri)
        {
            Content = new FormUrlEncodedContent(formContent)
        };

        // Add headers
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

        if (AdditionalHeaders != null)
        {
            foreach (string? key in AdditionalHeaders.AllKeys)
            {
                if (key != null)
                {
                    var value = AdditionalHeaders[key];
                    if (value != null)
                    {
                        request.Headers.TryAddWithoutValidation(key, value);
                    }
                }
            }
        }

        // Configure timeout
        using var timeoutCts = timeout.TotalMilliseconds > 0 && timeout != System.Threading.Timeout.InfiniteTimeSpan
            ? new CancellationTokenSource(timeout)
            : null;
        
        using var linkedCts = timeoutCts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;
        
        var effectiveToken = linkedCts?.Token ?? cancellationToken;

        try
        {
            // Get HttpClient (may be overridden for mTLS)
            var httpClient = GetHttpClient();
            
            // Send request
            using var response = await httpClient.SendAsync(request, effectiveToken);
            
            // Read response
            var responseBody = await response.Content.ReadAsStringAsync(effectiveToken);
            
            // Extract headers
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in response.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            Logger.Write("server", headers.GetValueOrDefault("x-server"));

            return new GSResponse(Method!, headers, responseBody, Logger);
        }
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Request to {resourceUri} timed out after {timeout.TotalSeconds} seconds");
        }
    }

    #endregion

    #region Helper Classes

    private class TaskAsyncResult : IAsyncResult
    {
        public Task<GSResponse> Task { get; }
        public object? AsyncState { get; }
        public WaitHandle AsyncWaitHandle => ((IAsyncResult)Task).AsyncWaitHandle;
        public bool CompletedSynchronously => ((IAsyncResult)Task).CompletedSynchronously;
        public bool IsCompleted => Task.IsCompleted;

        public TaskAsyncResult(Task<GSResponse> task, AsyncCallback? callback, object? state)
        {
            Task = task;
            AsyncState = state;
            if (callback != null)
                task.ContinueWith(_ => callback(this));
        }
    }

    #endregion
}