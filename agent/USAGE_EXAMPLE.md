# CronetClient Usage Examples

This document demonstrates how to use the new high-level `CronetClient` API.

---

## Basic Usage

### Simple GET Request

```csharp
using CronetSharp.Client;

// Create a client with default settings
using var client = new CronetClient();

// Make a GET request
var response = client.Get("https://api.github.com/users/github");

// Access response data
Console.WriteLine($"Status: {response.StatusCode} {response.StatusText}");
Console.WriteLine($"Body: {response.Body.AsString()}");

// Access headers
foreach (var header in response.Headers)
{
    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
}
```

### POST Request with Body

```csharp
using CronetSharp.Client;
using System.Text;

using var client = new CronetClient();

// Create request body
var jsonBody = Body.FromString("{\"name\": \"test\", \"value\": 42}");

// Make a POST request
var response = client.Post("https://httpbin.org/post", jsonBody);

Console.WriteLine($"Response: {response.Body.AsString()}");
```

---

## Advanced Usage

### Custom Configuration

```csharp
using CronetSharp;
using CronetSharp.Client;

// Create custom engine parameters
var engineParams = new CronetEngineParams
{
    UserAgent = "MyApp/1.0",
    EnableHttp2 = true,
    EnableQuic = true,
    EnableBrotli = true
};

// Create client with custom configuration
using var client = new CronetClient(engineParams);

// Configure timeout
client.DefaultTimeout = TimeSpan.FromSeconds(10);

// Make request
var response = client.Get("https://example.com");
```

### Custom Redirect Handling

```csharp
using var client = new CronetClient();

// Only follow redirects to specific domains
client.ShouldRedirect = url =>
{
    return url.Contains("example.com") || url.Contains("trusted-site.com");
};

var response = client.Get("https://example.com/redirect-test");
```

### Request with Custom Headers

```csharp
using var client = new CronetClient();

var response = client.Send(
    url: "https://api.example.com/data",
    method: "GET",
    body: null,
    headers: new[]
    {
        ("Authorization", "Bearer token123"),
        ("Accept", "application/json"),
        ("X-Custom-Header", "value")
    }
);
```

---

## Async/Await Support

### Async GET Request

```csharp
using var client = new CronetClient();

// Make async request
var response = await client.GetAsync("https://api.github.com/users/github");

Console.WriteLine($"Status: {response.StatusCode}");
Console.WriteLine($"Body: {response.Body.AsString()}");
```

### Async POST with Cancellation

```csharp
using var client = new CronetClient();
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    var body = Body.FromString("{\"data\": \"test\"}");
    var response = await client.PostAsync(
        "https://httpbin.org/post",
        body,
        cts.Token
    );

    Console.WriteLine($"Success: {response.StatusCode}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request was cancelled");
}
```

---

## Working with Different Body Types

### From Bytes

```csharp
byte[] data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
var body = Body.FromBytes(data);
var response = client.Post("https://httpbin.org/post", body);
```

### From String

```csharp
var body = Body.FromString("Hello, World!");
var response = client.Post("https://httpbin.org/post", body);
```

### From File

```csharp
var body = Body.FromFile("/path/to/file.txt");
var response = client.Post("https://httpbin.org/post", body);
```

### From Stream

```csharp
using var fileStream = File.OpenRead("/path/to/file.txt");
var body = Body.FromStream(fileStream);
var response = client.Post("https://httpbin.org/post", body);
```

---

## Error Handling

### Handling Client Errors

```csharp
using var client = new CronetClient();

try
{
    var response = client.Get("https://example.com/api");
    Console.WriteLine($"Success: {response.StatusCode}");
}
catch (ClientError ex) when (ex.IsTimeout)
{
    Console.WriteLine("Request timed out");
}
catch (ClientError ex) when (ex.IsCancellation)
{
    Console.WriteLine("Request was cancelled");
}
catch (ClientError ex) when (ex.IsCronetError)
{
    Console.WriteLine($"Cronet error: {ex.Message}");
    Console.WriteLine($"Error code: {ex.CronetError?.CronetErrorCode}");
}
catch (ClientError ex)
{
    Console.WriteLine($"Client error: {ex.Message}");
}
```

### Timeout Configuration

```csharp
using var client = new CronetClient();

// Set 5 second timeout
client.DefaultTimeout = TimeSpan.FromSeconds(5);

try
{
    // This will timeout if server takes > 5 seconds
    var response = client.Get("https://httpbin.org/delay/10");
}
catch (ClientError ex) when (ex.IsTimeout)
{
    Console.WriteLine("Request timed out after 5 seconds");
}

// Disable timeout
client.DefaultTimeout = null;
var response2 = client.Get("https://httpbin.org/delay/10"); // Will wait indefinitely
```

---

## Comparing to Low-Level API

### Low-Level API (Before)

```csharp
// Complex setup required
var engine = new CronetEngine();
var engineParams = new CronetEngineParams();
engine.Start();

var executor = Executors.NewSingleThreadExecutor();

var callback = new UrlRequestCallback
{
    OnResponseStarted = (request, info) => { /* handle */ },
    OnReadCompleted = (request, info, buffer, bytes) => { /* handle */ },
    OnSucceeded = (request, info) => { /* handle */ },
    OnFailed = (request, info, error) => { /* handle */ },
    OnCancelled = (request, info) => { /* handle */ }
};

var requestParams = new UrlRequestParams();
requestParams.SetHttpMethod("GET");

var urlRequest = engine.NewUrlRequest(
    "https://example.com",
    callback,
    executor,
    requestParams
);

urlRequest.Start();
// ... manual response handling, buffer management, etc.

// Manual cleanup
engine.Shutdown();
engine.Dispose();
executor.Dispose();
```

### High-Level API (New)

```csharp
// Simple, HttpClient-like API
using var client = new CronetClient();
var response = client.Get("https://example.com");
Console.WriteLine(response.Body.AsString());
// Automatic cleanup via using statement
```

---

## Performance Considerations

### Reusing Client Instance

```csharp
// ✅ GOOD: Reuse client for multiple requests
using var client = new CronetClient();

for (int i = 0; i < 100; i++)
{
    var response = await client.GetAsync($"https://api.example.com/item/{i}");
    ProcessResponse(response);
}

// ❌ BAD: Creating new client for each request
for (int i = 0; i < 100; i++)
{
    using var client = new CronetClient(); // Expensive!
    var response = await client.GetAsync($"https://api.example.com/item/{i}");
    ProcessResponse(response);
}
```

### Buffer Size Configuration (Advanced)

For response handlers that need custom buffer sizes (default is 512 bytes):

```csharp
// This is internal to the client, but ResponseHandler can be configured
// if you need fine-grained control over buffer sizes for large responses.
// Generally, the default 512 bytes is optimal for most use cases.
```

---

## Migration Guide

If you're currently using the low-level API, here's how to migrate:

### Before (Low-Level)

```csharp
var engine = new CronetEngine();
// ... engine setup ...
var urlRequest = engine.NewUrlRequest(url, callback, executor, params);
urlRequest.Start();
// ... manual response handling ...
```

### After (High-Level)

```csharp
using var client = new CronetClient();
var response = client.Get(url);
// Response data readily available
```

---

## Best Practices

1. **Use `using` statements** - Always wrap CronetClient in a using statement or call Dispose() explicitly
2. **Reuse client instances** - Create one client and use it for multiple requests
3. **Configure timeouts** - Set appropriate timeouts based on your use case
4. **Handle errors gracefully** - Use try-catch with specific ClientError types
5. **Use async methods** - Prefer GetAsync/PostAsync for non-blocking operations
6. **Custom redirect logic** - Implement ShouldRedirect for security-sensitive applications

---

## Full Example Application

```csharp
using System;
using System.Threading.Tasks;
using CronetSharp;
using CronetSharp.Client;

class Program
{
    static async Task Main(string[] args)
    {
        // Create client with custom settings
        var engineParams = new CronetEngineParams
        {
            UserAgent = "MyApp/1.0",
            EnableHttp2 = true,
            EnableQuic = true
        };

        using var client = new CronetClient(engineParams);

        // Configure client
        client.DefaultTimeout = TimeSpan.FromSeconds(10);
        client.ShouldRedirect = url => url.StartsWith("https://");

        try
        {
            // Make GET request
            Console.WriteLine("Fetching GitHub user...");
            var response = await client.GetAsync("https://api.github.com/users/github");

            Console.WriteLine($"Status: {response.StatusCode} {response.StatusText}");
            Console.WriteLine($"Protocol: {response.NegotiatedProtocol}");
            Console.WriteLine($"Cached: {response.WasCached}");
            Console.WriteLine("\nResponse Body:");
            Console.WriteLine(response.Body.AsString());

            // Make POST request
            Console.WriteLine("\n\nMaking POST request...");
            var postBody = Body.FromString("{\"test\": \"data\"}");
            var postResponse = await client.PostAsync(
                "https://httpbin.org/post",
                postBody
            );

            Console.WriteLine($"POST Status: {postResponse.StatusCode}");
            Console.WriteLine($"POST Response: {postResponse.Body.AsString()}");
        }
        catch (ClientError ex) when (ex.IsTimeout)
        {
            Console.WriteLine("Request timed out");
        }
        catch (ClientError ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}
```

---

## Differences from HttpClient

While CronetClient provides a similar API to HttpClient, there are some differences:

| Feature | HttpClient | CronetClient |
|---------|-----------|--------------|
| Engine | .NET HTTP stack | Chromium Cronet |
| HTTP/2 Support | ✅ | ✅ |
| QUIC/HTTP/3 Support | ❌ (limited) | ✅ |
| Brotli Compression | ✅ | ✅ |
| Request Headers | HttpRequestHeaders | (string, string)[] tuples |
| Response Type | HttpResponseMessage | HttpResponse |
| Async Only | ✅ | ✅ + Sync methods |
| Redirect Control | HttpClientHandler | ShouldRedirect delegate |

---

**For more examples, see the `/CronetSharp.Example/Examples/` directory.**
