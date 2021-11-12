---
parent: Understand and Extend the Kiota generator
---

# Kiota Abstractions

On most platforms there are a range of different HTTP client library implementations. Developers often have preferences on which is the best implementation to meet their needs. Kiota's objective is to make it easier to create a HTTP request object but attempts to be agnostic of the library that will make the HTTP call. In order to decouple Kiota from specific HTTP libraries, we have defined a set of abstractions.

## HTTP Core

The HTTP core interface is the primary point where Kiota service libraries will trigger the creation of a HTTP request.  Below is the [C# implementation](https://github.com/microsoft/kiota/blob/main/abstractions/dotnet/src/IRequestAdapter.cs).

```csharp
public interface IRequestAdapter
{
    void EnableBackingStore(IBackingStoreFactory backingStoreFactory);

    ISerializationWriterFactory SerializationWriterFactory { get; }

    Task<ModelType> SendAsync<ModelType>(
        RequestInformation requestInfo,
        IResponseHandler responseHandler = default) where ModelType : IParsable;


    Task<IEnumerable<ModelType>> SendCollectionAsync<ModelType>(
        RequestInformation requestInfo,
        IResponseHandler responseHandler = default) where ModelType : IParsable;


    Task<ModelType> SendPrimitiveAsync<ModelType>(
        RequestInformation requestInfo,
        IResponseHandler responseHandler = default);

    Task SendNoContentAsync(
        RequestInformation requestInfo,
        IResponseHandler responseHandler = default);
}
```

Kiota service libraries return the model type that is associated with HTTP resource. This behavior can be overridden by changing the `responseHandler` to do something different than default behavior.  One use of this is to change the response type to be either a native HTTP response class, or return a generic API response class that provides access to more underlying metadata.

## RequestInformation

In order to enable Kiota service libraries to make requests, they need to be able accumulate information about the request and pass it to the core library. The RequestInformation object is designed to do that. It only contains properties that be provided by the request builders. As request builders get more sophisticated, so may the RequestInformation class.

```csharp
public class RequestInformation
{
    public Uri URI { get; set; }
    public HttpMethod HttpMethod { get; set; }
    public IDictionary<string, object> QueryParameters { get; set; } =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, string> Headers { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Stream Content { get; set; }
}
```

```typescript
export interface RequestInformation {
    URI?: URL;
    httpMethod?: HttpMethod;
    content?: ReadableStream;
    queryParameters?: Map<string, object>;
    headers?: Map<string, string>;
}
```

```java
public class RequestInformation {
    @Nullable
    public URI uri;
    @Nullable
    public HttpMethod httpMethod;
    @Nonnull
    //TODO case insensitive
    public HashMap<String, Object> queryParameters = new HashMap<>();
    @Nonnull
    // TODO case insensitive
    public HashMap<String, String> headers = new HashMap<>();
    @Nullable
    public InputStream Content;
}
```

## ResponseHandler

TBD

- This allows core to do all the default hard work, but enables a custom response handler to change the behavior of the method.

```csharp
public interface IResponseHandler
{
    Task<ModelType> HandleResponseAsync<NativeResponseType, ModelType>(NativeResponseType response);
}
```
