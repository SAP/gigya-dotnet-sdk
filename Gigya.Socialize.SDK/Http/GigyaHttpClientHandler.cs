/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GigyaHttpClientHandler
 */

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gigya.Socialize.SDK.Http;

/// <summary>
/// A custom HttpClientHandler that supports mTLS and logging.
/// </summary>
public class GigyaHttpClientHandler : HttpClientHandler
{
    private readonly ILogger _logger;
    private X509Certificate2? _clientCertificate;

    /// <summary>
    /// Creates a new GigyaHttpClientHandler with optional logging.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public GigyaHttpClientHandler(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        
        // Enable automatic decompression
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
        
        // Configure connection pooling
        MaxConnectionsPerServer = GSRequest.MaxConcurrentConnections;
        
        // Configure SSL/TLS
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
    }

    /// <summary>
    /// Sets the client certificate for mTLS authentication.
    /// </summary>
    /// <param name="certificate">The client certificate.</param>
    public void SetClientCertificate(X509Certificate2 certificate)
    {
        _clientCertificate = certificate;
        ClientCertificates.Clear();
        ClientCertificates.Add(certificate);
        _logger.LogDebug("Client certificate set: {Subject}", certificate.Subject);
    }

    /// <summary>
    /// Clears the client certificate.
    /// </summary>
    public void ClearClientCertificate()
    {
        _clientCertificate = null;
        ClientCertificates.Clear();
        _logger.LogDebug("Client certificate cleared");
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending {Method} request to {Uri}", request.Method, request.RequestUri);
        
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            _logger.LogDebug("Received response: {StatusCode}", response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request to {Uri} failed", request.RequestUri);
            throw;
        }
    }
}