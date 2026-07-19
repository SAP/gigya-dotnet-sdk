/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - IGigyaHttpClient
 */

using System.Security.Cryptography.X509Certificates;

namespace Gigya.Socialize.SDK.Http;

/// <summary>
/// Interface for the Gigya HTTP client that provides modern async HTTP operations.
/// </summary>
public interface IGigyaHttpClient : IDisposable
{
    /// <summary>
    /// Sends a POST request asynchronously.
    /// </summary>
    /// <param name="uri">The request URI.</param>
    /// <param name="content">The form content to send.</param>
    /// <param name="headers">Optional additional headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response.</returns>
    Task<GigyaHttpResponse> PostAsync(
        string uri,
        Dictionary<string, string> content,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request asynchronously with mTLS authentication.
    /// </summary>
    /// <param name="uri">The request URI.</param>
    /// <param name="content">The form content to send.</param>
    /// <param name="clientCertificate">The client certificate for mTLS.</param>
    /// <param name="headers">Optional additional headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response.</returns>
    Task<GigyaHttpResponse> PostWithMtlsAsync(
        string uri,
        Dictionary<string, string> content,
        X509Certificate2 clientCertificate,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets the timeout for requests.
    /// </summary>
    TimeSpan Timeout { get; set; }
}

/// <summary>
/// Represents an HTTP response from the Gigya API.
/// </summary>
public sealed class GigyaHttpResponse
{
    /// <summary>
    /// The response body as a string.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// The response headers.
    /// </summary>
    public required Dictionary<string, string> Headers { get; init; }

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Whether the request was successful (2xx status code).
    /// </summary>
    public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode < 300;
}