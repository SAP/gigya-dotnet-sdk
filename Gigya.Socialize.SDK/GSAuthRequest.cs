/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GSAuthRequest
 */

using System.Collections.Specialized;
using System.Net;

namespace Gigya.Socialize.SDK;

/// <summary>
/// A request class that uses JWT Bearer authentication with RSA-signed tokens.
/// Suitable for calling global sites REST API.
/// </summary>
public class GSAuthRequest : GSRequest
{
    private readonly string? _privateKey;

    /// <summary>
    /// Constructs a request using a userKey and privateKey.
    /// Suitable for calling global sites REST API.
    /// </summary>
    /// <param name="userKey">An administrative user's key.</param>
    /// <param name="privateKey">An administrative user's private key (PEM). Usually read from file.</param>
    /// <param name="apiKey">Gigya's API key obtained from Site-Setup page on the Gigya website.</param>
    /// <param name="apiMethod">The API method (including namespace) to call. For example: socialize.getUserInfo
    /// If namespace is not supplied "socialize" is assumed.</param>
    /// <param name="clientParams">The request parameters.</param>
    /// <param name="additionalHeaders">A collection of additional headers for the HTTP request.</param>
    /// <param name="proxy">Proxy for the HTTP request.</param>
    public GSAuthRequest(
        string userKey,
        string privateKey,
        string? apiKey,
        string apiMethod,
        object? clientParams = null,
        NameValueCollection? additionalHeaders = null,
        IWebProxy? proxy = null)
        : base(apiKey, null, apiMethod, clientParams, true, userKey, additionalHeaders, proxy)
    {
        if (string.IsNullOrEmpty(userKey))
        {
            Logger.Write(new MissingFieldException("Authorized request must have userKey"));
            return;
        }
        _privateKey = privateKey;
    }

    /// <summary>
    /// Validates the request before sending.
    /// </summary>
    protected override bool IsValidRequest()
    {
        return !string.IsNullOrEmpty(Method) && !string.IsNullOrEmpty(UserKey);
    }

    /// <summary>
    /// Sets default parameters before signing.
    /// </summary>
    protected override void SetDefaultParams(string httpMethod, string resourceUri)
    {
        if (ApiKey != null)
            SetParam("apiKey", ApiKey);
    }

    /// <summary>
    /// Signs the request using JWT Bearer authentication.
    /// </summary>
    protected override void Sign(string httpMethod, string resourceUri)
    {
        if (string.IsNullOrEmpty(UserKey))
        {
            Logger.Write(new MissingFieldException("Failed to sign request, missing userKey"));
            return;
        }
        
        if (string.IsNullOrEmpty(_privateKey))
        {
            Logger.Write(new MissingFieldException("Failed to sign request, missing privateKey"));
            return;
        }

        AdditionalHeaders ??= new NameValueCollection();
        AdditionalHeaders["Authorization"] = SigUtils.CalcAuthorizationBearer(UserKey, _privateKey);
    }
}