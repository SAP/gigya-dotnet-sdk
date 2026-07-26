# SAP Customer Data Cloud .NET SDK

Modern .NET 9 SDK for SAP Customer Data Cloud (Gigya) API.

## Description

The .NET SDK provides a C# interface for the SAP Customer Data Cloud (Gigya) API. This is a modern .NET 9 implementation that maintains full API compatibility with the legacy SDK while adopting modern .NET best practices.

## Requirements

- [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) or later

## Features

- **Full API Compatibility**: Drop-in replacement for the legacy SDK
- **Modern .NET 9**: Built with the latest .NET features and best practices
- **Nullable Reference Types**: Full nullable annotations for better null safety
- **Async/Await Support**: Native async methods with `CancellationToken` support
- **mTLS Authentication**: Support for mutual TLS authentication with client certificates
- **Dependency Injection**: Built-in support for `Microsoft.Extensions.DependencyInjection`

## Download and Installation

### From Source

```bash
# Clone the repository
git clone https://github.com/SAP/gigya-dotnet-sdk.git

# Build the solution
cd gigya-dotnet-sdk
dotnet build

# Run tests
dotnet test
```

### NuGet Package

```bash
dotnet add package Gigya.Socialize.SDK
```

## Quick Start

### Basic Usage

```csharp
using Gigya.Socialize.SDK;

// Create a request with API key and secret
var request = new GSRequest("your-api-key", "your-secret-key", "accounts.getAccountInfo");
request.SetParam("UID", "user-uid");

// Send the request
GSResponse response = request.Send();

// Check the response
if (response.GetErrorCode() == 0)
{
    Console.WriteLine(response.GetResponseText());
}
```

### Authenticated Request (UserKey + PrivateKey)

```csharp
using Gigya.Socialize.SDK;

// Create an authenticated request
var request = new GSAuthRequest(
    "your-user-key",
    "your-private-key",
    "your-api-key",
    "admin.getUserKey"
);
request.APIDomain = "us1.gigya.com";
request.SetParam("getSelf", true);

// Send the request
GSResponse response = request.Send();
```

### mTLS Authentication

```csharp
using Gigya.Socialize.SDK;

// Load mTLS configuration from PEM files
MtlsConfig mtlsConfig = MtlsConfig.FromFiles(
    "path/to/client.pem",
    "path/to/client.key"
);

// Create an mTLS request
var request = new GSAuthMtlsRequest(
    "your-api-key",
    "accounts.getAccountInfo",
    mtlsConfig
);
request.SetParam("UID", "user-uid");

// Send the request
GSResponse response = request.Send();
```

### Async Usage

```csharp
using Gigya.Socialize.SDK;

// Create a request
var request = new GSAuthRequest(
    "your-user-key",
    "your-private-key",
    "your-api-key",
    "accounts.getAccountInfo"
);
request.SetParam("UID", "user-uid");

// Send asynchronously with cancellation support
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
GSResponse response = await request.SendAsync(cts.Token);
```

## Configuration

1. [Obtain a Gigya APIKey and Secret key](https://developers.gigya.com/display/GD/.NET#id-.NET-ObtainingGigya'sAPIKeyandSecretkey)
2. Include the Gigya SDK namespace in your C# source:

```csharp
using Gigya.Socialize.SDK;
```

3. Start using according to [documentation](http://developers.gigya.com/display/GD/.NET)

## API Reference

### Core Classes

| Class | Description |
|-------|-------------|
| `GSRequest` | Basic API request with API key and secret |
| `GSAuthRequest` | Authenticated request with UserKey and PrivateKey |
| `GSAuthMtlsRequest` | mTLS authenticated request with client certificate |
| `GSResponse` | API response wrapper |
| `GSObject` | Dynamic JSON-like object for parameters |
| `GSArray` | Array container for GSObject items |
| `GSException` | SDK exception class |

### Utility Classes

| Class | Description |
|-------|-------------|
| `SigUtils` | Signature validation utilities |
| `MtlsConfig` | mTLS certificate configuration |
| `GSLogger` | Logging utilities |

## Datacenter Domains

| Datacenter | Domain |
|------------|--------|
| US1 | `us1.gigya.com` |
| EU1 | `eu1.gigya.com` |
| AU1 | `au1.gigya.com` |
| IL3 | `il3.gigya.com` |
| CN1 | `cn1.sapcdm.cn` |

## Migration from Legacy SDK

This SDK is a drop-in replacement for the legacy .NET SDK. The public API is fully compatible. Key differences:

1. **Target Framework**: .NET 9.0 (instead of .NET Framework 4.5+)
2. **Nullable Reference Types**: All APIs have nullable annotations
3. **Async Methods**: New `SendAsync()` methods with `CancellationToken` support
4. **Modern Patterns**: Uses modern .NET patterns internally

### Breaking Changes

None. The public API is fully backward compatible.

### Deprecated APIs

Some legacy methods are marked with `[Obsolete]` but remain functional:
- `GSSession.getAccessToken()` → Use `GSSession.AccessToken` property
- `GSSession.setAccessToken()` → Use `GSSession.AccessToken` property
- `GSRequest.Send()` → Consider using `SendAsync()` for new code

## Limitations

None

## Known Issues

None

## How to Obtain Support

[Learn more](https://help.sap.com/viewer/8b8d6fffe113457094a17701f63e3d6a/GIGYA/en-US/4167e8a470b21014bbc5a10ce4041860.html)

## Contributing

Via pull request to this repository. See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## Code of Conduct

See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)

## Licensing

Please see our [LICENSE](LICENSE) for copyright and license information.

Licensed under the Apache License, Version 2.0.