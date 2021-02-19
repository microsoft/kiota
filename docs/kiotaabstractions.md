# Kiota Abstractions

On most platforms there are a range of different HTTP client library implementations. Developers often have preferences on which is the best implementation to meet their needs. Kiota's objective is to make it easier to create a HTTP request object but attempts to be agnostic of the library that will make the HTTP call. In order to decouple Kiota from specific HTTP libraries, we have defined a set of abstractions.

## HTTP Core

The HTTP core interface is the primary point where Kiota service libraries will trigger the creation of a HTTP request.

```csharp
    public interface IHttpCore<TNativeResponse>
    {
        Task<Stream> SendAsync(RequestInfo requestInfo);
        Task<TNativeResponse> SendNativeAsync(RequestInfo requestInfo);
    }
```

Kiota service libraries support two ways to access the response of an HTTP call. The first is the simplest and assumes that the core library will process the HTTP status codes and only return a stream of the response body. The service library will process this stream and deserialize into the appropriate model object. If an error is detected in core, an exception will be thrown up to be caught by the calling application. This is the classic RPC model.

The second method returns a "native response object" that allows the caller direct access to the HTTP response. This type of method is provided to deal with scenarios where standard RPC behavior is not appropriate for handling the response.  The type of the native response object is defined by the core library and is the same type for all request builders. This type is provided to the code generator to ensure the native methods are generated correctly.

## RequestInfo

In order to enable Kiota service libraries to make requests, they need to be able accumulate information about the request and pass it to the core library. The RequestInfo object is designed to do that. It only contains properties that be provided by the request builders. As request builders get more sophisticated, so may the RequestInfo class.

```csharp
public class RequestInfo
{
    public string Path;
    public string HttpMethod;
    public IDictionary<string, object> QueryParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Stream Content;
}
```
