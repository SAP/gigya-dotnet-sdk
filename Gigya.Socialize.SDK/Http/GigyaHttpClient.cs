/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GigyaHttpClient
 */

using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gigya.Socialize.SDK.Http;

/// <summary>
/// Modern HttpClient-based implementation of IGigyaHttpClient.
/// </summary>
public sealed class GigyaHttpClient : IGigyaHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GigyaHttpClient> _logger;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    // Lazy-initialized mTLS client (created on demand)
    private HttpClient? _mtlsHttpClient;
    private X509Certificate2? _currentMtlsCertificate;
    private readonly object _mtlsLock = new();

    /// <summary>
    /// Creates a new GigyaHttpClient with a default HttpClient.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public GigyaHttpClient(ILogger<GigyaHttpClient>? logger = null)
        : this(CreateDefaultHttpClient(), logger, ownsHttpClient: true)
    {
    }

    /// <summary>
    /// Creates a new GigyaHttpClient with the specified HttpClient.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <param name="ownsHttpClient">Whether this instance owns the HttpClient and should dispose it.</param>
    public GigyaHttpClient(HttpClient httpClient, ILogger<GigyaHttpClient>? logger = null, bool ownsHttpClient = false)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullLogger<GigyaHttpClient>.Instance;
        _ownsHttpClient = ownsHttpClient;
    }

    /// <inheritdoc />
    public TimeSpan Timeout
    {
        get => _httpClient.Timeout;
        set => _httpClient.Timeout = value;
    }

    /// <inheritdoc />
    public async Task<GigyaHttpResponse> PostAsync(
        string uri,
        Dictionary<string, string> content,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogDebug("Sending POST request to {Uri}", uri);

        using var request = CreateRequest(HttpMethod.Post, uri, content, headers);
        
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return await CreateResponseAsync(response, cancellationToken);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Request to {Uri} was cancelled", uri);
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request to {Uri} timed out", uri);
            throw new TimeoutException($"Request to {uri} timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to {Uri} failed", uri);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<GigyaHttpResponse> PostWithMtlsAsync(
        string uri,
        Dictionary<string, string> content,
        X509Certificate2 clientCertificate,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogDebug("Sending mTLS POST request to {Uri}", uri);

        var mtlsClient = GetOrCreateMtlsClient(clientCertificate);
        using var request = CreateRequest(HttpMethod.Post, uri, content, headers);

        try
        {
            using var response = await mtlsClient.SendAsync(request, cancellationToken);
            return await CreateResponseAsync(response, cancellationToken);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("mTLS request to {Uri} was cancelled", uri);
            throw;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("mTLS request to {Uri} timed out", uri);
            throw new TimeoutException($"mTLS request to {uri} timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "mTLS HTTP request to {Uri} failed", uri);
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }

        lock (_mtlsLock)
        {
            _mtlsHttpClient?.Dispose();
            _mtlsHttpClient = null;
            _currentMtlsCertificate = null;
        }
    }

    #region Private Methods

    private static HttpClient CreateDefaultHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            MaxConnectionsPerServer = GSRequest.MaxConcurrentConnections,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true
        };

        return new HttpClient(handler)
        {
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };
    }

    private HttpClient GetOrCreateMtlsClient(X509Certificate2 certificate)
    {
        lock (_mtlsLock)
        {
            // Check if we can reuse the existing client
            if (_mtlsHttpClient != null && _currentMtlsCertificate != null)
            {
                // Compare certificates by thumbprint
                if (_currentMtlsCertificate.Thumbprint == certificate.Thumbprint)
                {
                    return _mtlsHttpClient;
                }

                // Different certificate, dispose old client
                _mtlsHttpClient.Dispose();
            }

            // Create new mTLS client
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                MaxConnectionsPerServer = GSRequest.MaxConcurrentConnections,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    ClientCertificates = new X509Certificate2Collection { certificate },
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                }
            };

            _mtlsHttpClient = new HttpClient(handler)
            {
                Timeout = _httpClient.Timeout
            };
            _currentMtlsCertificate = certificate;

            _logger.LogDebug("Created new mTLS HttpClient with certificate: {Thumbprint}", certificate.Thumbprint);

            return _mtlsHttpClient;
        }
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string uri,
        Dictionary<string, string> content,
        Dictionary<string, string>? headers)
    {
        var request = new HttpRequestMessage(method, uri)
        {
            Content = new FormUrlEncodedContent(content)
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

        if (headers != null)
        {
            foreach (var (key, value) in headers)
            {
                // Some headers need to be set on Content, not Request
                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    // FormUrlEncodedContent already sets this
                    continue;
                }

                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        return request;
    }

    private static async Task<GigyaHttpResponse> CreateResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        return new GigyaHttpResponse
        {
            Body = body,
            Headers = headers,
            StatusCode = (int)response.StatusCode
        };
    }

    #endregion
}