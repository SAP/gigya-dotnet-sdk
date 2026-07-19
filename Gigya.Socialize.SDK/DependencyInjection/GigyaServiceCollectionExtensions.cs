/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - DI Extensions
 */

using Gigya.Socialize.SDK.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Gigya.Socialize.SDK.DependencyInjection;

/// <summary>
/// Extension methods for registering Gigya SDK services with dependency injection.
/// </summary>
public static class GigyaServiceCollectionExtensions
{
    /// <summary>
    /// The name of the HttpClient used by the Gigya SDK.
    /// </summary>
    public const string HttpClientName = "GigyaSDK";

    /// <summary>
    /// Adds Gigya SDK services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGigyaSdk(this IServiceCollection services)
    {
        return services.AddGigyaSdk(configure: null);
    }

    /// <summary>
    /// Adds Gigya SDK services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for the HttpClient.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGigyaSdk(
        this IServiceCollection services,
        Action<HttpClient>? configure)
    {
        // Register the named HttpClient with IHttpClientFactory
        var builder = services.AddHttpClient(HttpClientName, client =>
        {
            // Default configuration
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            // Apply custom configuration
            configure?.Invoke(client);
        });

        // Configure the primary handler with connection pooling
        builder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            MaxConnectionsPerServer = GSRequest.MaxConcurrentConnections,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true
        });

        // Register IGigyaHttpClient
        services.TryAddSingleton<IGigyaHttpClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetService<ILogger<GigyaHttpClient>>();
            var httpClient = factory.CreateClient(HttpClientName);
            return new GigyaHttpClient(httpClient, logger, ownsHttpClient: false);
        });

        // Register GigyaRequestFactory for creating requests
        services.TryAddSingleton<IGigyaRequestFactory, GigyaRequestFactory>();

        return services;
    }

    /// <summary>
    /// Adds Gigya SDK services with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Configuration action for GigyaOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGigyaSdkWithOptions(
        this IServiceCollection services,
        Action<GigyaOptions> configureOptions)
    {
        var options = new GigyaOptions();
        configureOptions(options);

        services.AddSingleton(options);

        Action<HttpClient>? httpClientConfig = options.Timeout.HasValue
            ? client => client.Timeout = options.Timeout.Value
            : null;

        return services.AddGigyaSdk(httpClientConfig);
    }
}

/// <summary>
/// Configuration options for the Gigya SDK.
/// </summary>
public class GigyaOptions
{
    /// <summary>
    /// The API key for Gigya requests.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The secret key for signing requests.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// The user key for administrative operations.
    /// </summary>
    public string? UserKey { get; set; }

    /// <summary>
    /// The private key (PEM format) for JWT authentication.
    /// </summary>
    public string? PrivateKey { get; set; }

    /// <summary>
    /// The API domain (e.g., "us1.gigya.com").
    /// </summary>
    public string ApiDomain { get; set; } = "us1.gigya.com";

    /// <summary>
    /// The request timeout.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Whether to use HTTPS for requests.
    /// </summary>
    public bool UseHttps { get; set; } = true;
}

/// <summary>
/// Factory interface for creating Gigya requests.
/// </summary>
public interface IGigyaRequestFactory
{
    /// <summary>
    /// Creates a new GSRequest.
    /// </summary>
    /// <param name="apiMethod">The API method to call.</param>
    /// <returns>A new GSRequest instance.</returns>
    GSRequest CreateRequest(string apiMethod);

    /// <summary>
    /// Creates a new GSRequest with parameters.
    /// </summary>
    /// <param name="apiMethod">The API method to call.</param>
    /// <param name="parameters">The request parameters.</param>
    /// <returns>A new GSRequest instance.</returns>
    GSRequest CreateRequest(string apiMethod, GSObject parameters);

    /// <summary>
    /// Creates a new GSAuthRequest for JWT authentication.
    /// </summary>
    /// <param name="apiMethod">The API method to call.</param>
    /// <returns>A new GSAuthRequest instance.</returns>
    GSAuthRequest CreateAuthRequest(string apiMethod);

    /// <summary>
    /// Creates a new GSAuthMtlsRequest for mTLS authentication.
    /// </summary>
    /// <param name="apiMethod">The API method to call.</param>
    /// <param name="mtlsConfig">The mTLS configuration.</param>
    /// <returns>A new GSAuthMtlsRequest instance.</returns>
    GSAuthMtlsRequest CreateMtlsRequest(string apiMethod, MtlsConfig mtlsConfig);
}

/// <summary>
/// Default implementation of IGigyaRequestFactory.
/// </summary>
internal sealed class GigyaRequestFactory : IGigyaRequestFactory
{
    private readonly GigyaOptions _options;
    private readonly ILogger<GigyaRequestFactory> _logger;

    public GigyaRequestFactory(GigyaOptions options, ILogger<GigyaRequestFactory>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GigyaRequestFactory>.Instance;
    }

    public GSRequest CreateRequest(string apiMethod)
    {
        ValidateBasicOptions();
        
        var request = new GSRequest(_options.ApiKey!, _options.SecretKey, apiMethod, null, _options.UseHttps);
        ConfigureRequest(request);
        return request;
    }

    public GSRequest CreateRequest(string apiMethod, GSObject parameters)
    {
        ValidateBasicOptions();
        
        var request = new GSRequest(_options.ApiKey!, _options.SecretKey, apiMethod, parameters, _options.UseHttps);
        ConfigureRequest(request);
        return request;
    }

    public GSAuthRequest CreateAuthRequest(string apiMethod)
    {
        if (string.IsNullOrEmpty(_options.UserKey))
            throw new InvalidOperationException("UserKey is required for JWT authentication");
        if (string.IsNullOrEmpty(_options.PrivateKey))
            throw new InvalidOperationException("PrivateKey is required for JWT authentication");

        var request = new GSAuthRequest(_options.UserKey, _options.PrivateKey, _options.ApiKey, apiMethod);
        ConfigureRequest(request);
        return request;
    }

    public GSAuthMtlsRequest CreateMtlsRequest(string apiMethod, MtlsConfig mtlsConfig)
    {
        var request = new GSAuthMtlsRequest(_options.ApiKey, apiMethod, mtlsConfig);
        ConfigureRequest(request);
        return request;
    }

    private void ValidateBasicOptions()
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new InvalidOperationException("ApiKey is required");
    }

    private void ConfigureRequest(GSRequest request)
    {
        request.APIDomain = _options.ApiDomain;
    }
}