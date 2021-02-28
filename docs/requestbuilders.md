# Request Builder

The primary goal of a request builder class is to enable a developer to easily construct an HTTP request. By using a set of related request builder classes we can enable a developer to contruct a HTTP request for any resource exposed by an HTTP API.

There are three phases of constructing the HTTP request. Building the path, specifying query parameters and selecting the HTTP method.

## Building the Path


By creating properties on request builder classes, the developer can effectively construct a URL path by navigating the hierarchy of request builders, aided by the IDE's autocomplete.

```csharp

    var todo = todoClient.Todos["<todoId>"].GetAsync();
    var responsiblePerson = todoClient.Todos["<todoId>"].AssignedTo.GetAsync();
```

Each request builder class exposes the set of HTTP methods that are supported on that resource. Each operation method allows setting configuring query parameters, setting HTTP headers and providing a custom response handler.
