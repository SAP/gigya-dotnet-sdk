# .NET SDK 
[Learn more](https://help.sap.com/viewer/8b8d6fffe113457094a17701f63e3d6a/GIGYA/en-US/41668f3970b21014bbc5a10ce4041860.html)

## Description
The .NET library provides C# interface for the Gigya API.
The library makes it simple to integrate Gigya's service in your .NET application.

## Requirements
[.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) or later

## Download and Installation
* Clone the repo.
* Open the solution.
* Build.

## Configuration
* [Obtain a Gigya APIKey and Secret key](https://github.com/SAP/gigya-dotnet-sdk/wiki#obtaining-gigyas-api-key-and-secret-key).
* Include the Gigya SDK namespace in your C# source:
```C#
using Gigya.Socialize.SDK;
```
* Start using according to [documentation](https://github.com/SAP/gigya-dotnet-sdk/wiki).

## Using GSAuthMtlsRequest (Mutual TLS Authentication)

For high-security server-to-server communication, use `GSAuthMtlsRequest` with a client X.509 certificate instead of (or alongside) the site secret. When mTLS is configured, the SDK routes the request to the datacenter-specific endpoint `mtls.{datacenter}.gigya.com` and presents the certificate during the TLS handshake.

### Example with Certificate Files

```C#
using Gigya.Socialize.SDK;

// Create mTLS config with certificate file paths
MtlsConfig config = MtlsConfig.FromFiles(
    "certs/client.pem",    // Path to client certificate
    "certs/client.key"     // Path to private key
);

// Create and send the request
var request = new GSAuthMtlsRequest(
    "your-api-key",
    "accounts.getAccountInfo",
    config
);

request.SetParam("UID", "user123");
GSResponse response = request.Send();

if (response.GetErrorCode() == 0)
{
    Console.WriteLine("Success: " + response.GetResponseText());
}
```

### Example with PEM Strings (Environment Variables)

You can also pass certificate and key content directly as PEM strings, which is useful when loading from environment variables or a secret store.

```C#
// Load certificates from environment variables
string certPem = Environment.GetEnvironmentVariable("MTLS_CERT_PEM");
string keyPem  = Environment.GetEnvironmentVariable("MTLS_KEY_PEM");

MtlsConfig config = MtlsConfig.FromPem(certPem, keyPem);

var request = new GSAuthMtlsRequest(
    Environment.GetEnvironmentVariable("GIGYA_API_KEY"),
    "accounts.getAccountInfo",
    config
);

request.SetParam("UID", "user123");
GSResponse response = request.Send();
```

If the private key is encrypted, pass the password to the `MtlsConfig` factory:

```C#
MtlsConfig config = MtlsConfig.FromFiles(
    "certs/client.pem",
    "certs/client.key",
    "key-password".ToCharArray()
);
```

**Note:** Both the certificate and key must be provided together. Each can be either a file path or PEM content.

## Limitations
None

## Known Issues
None

## How to obtain support
[Learn more](https://help.sap.com/viewer/8b8d6fffe113457094a17701f63e3d6a/GIGYA/en-US/4167e8a470b21014bbc5a10ce4041860.html)

## Contributing
Via pull request to this repository.

## Code of Conduct
See [CODE_OF_CONDUCT](https://github.com/SAP/gigya-dotnet-sdk/blob/main/CODE_OF_CONDUCT.md)

## To-Do (upcoming changes)
None

## Licensing
Please see our [LICENSE](https://github.com/SAP/gigya-dotnet-sdk/blob/main/LICENSE.txt) for copyright and license information.

[![REUSE status](https://api.reuse.software/badge/github.com/SAP/gigya-dotnet-sdk)](https://api.reuse.software/info/github.com/SAP/gigya-dotnet-sdk)
