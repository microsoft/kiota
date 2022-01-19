---
parent: Kiota deep dive
---

# Request builder

The primary goal of a request builder class is to enable a developer to easily construct an HTTP request. By using a set of related request builder classes we can enable a developer to construct an HTTP request for any resource exposed by an HTTP API.

There are three phases of constructing the HTTP request. Building the URL, specifying query parameters and selecting the HTTP method.

## Building the URL

By creating properties on request builder classes, the developer can effectively construct a URL by navigating the hierarchy of request builders, aided by the IDE's autocomplete.

The URL for the request is built using an [RFC6570 URL Template](https://datatracker.ietf.org/doc/html/rfc6570) and providing its parameters.

```csharp
var todo = await todoClient.Todos["<todoId>"].GetAsync();
var responsiblePerson =  await todoClient.Todos["<todoId>"].AssignedTo.GetAsync();
```

Each request builder class exposes the set of HTTP methods that are supported on that resource. Each operation method allows setting and configuring query parameters, setting HTTP headers and providing a custom response handler.

Request builders will be generated into a sub-namespace following the API path the refer to.

## Default members

Each request builder contains a default set of members to help build the URL and ultimately the requests.

### PathParameters

The Path Parameters dictionary/map contains a list of the parameters for the URL template which is built by the fluent API and passed along from request builder to request builder.

Considering the following sample:

```csharp
var requestBuilder = todoClient.TaskLists["taskListId"].Todos["todoId"].AssignedTo;
```

PathParameters for the requestBuilder variable will contain two entries:

- `task_list_id` of value `taskListId`.
- `todo_id` of value `todoId`.

When the request is executed by the request adapter service, all parameters (path and query) are resolved against the url template, the base url parameter value is also provided at that time.

### UrlTemplate

The request builder has a Url Template property. This template is resolved using the path and query parameters to get the definitive URL to use for the request.

Consider the following sample:

```csharp
var requestBuilder = todoClient.TaskLists["taskListId"].Todos["todoId"].AssignedTo;
```

The URL template for the requestBuilder will look something like `{+baseurl}/taskLists/{task_list_id}/todos/{todo_id}/assignedTo{?select,expand}`

> **Note:** the query parameters will only be present if included in the OpenAPI description.

The path and query parameters, as well as the base url will be provided by the request adapter before the request is executed.

### RequestAdapter

In order to execute any request, the request builder needs to have access to a request adapter.

This request adapter is passed from request builder to request builder when using the fluent style API. This object holds most of the client configuration, the serialization and deserialization capabilities and more.

### Constructors

Request builders offer two different constructors (one if the target language supports types unions).

```csharp
public UsersRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter);
public UsersRequestBuilder(string rawUrl, IRequestAdapter requestAdapter)
```

The first constructor (dictionary) is meant to be used by the fluent API infrastructure to pass in the path parameters from previous request builders.

The second constructor is meant to be used by developers when they need to use the request builder with a raw URL they already have. Multiple scenarios require such use: delta links, paging through collections, resuming from going offline...

> Note: once the raw URL is set for a given request builder, and subsequently for a given request, the path and query parameters on the request information will be reset, and the request adapter will not attempt to resolve the template anymore, it will read the provided raw URL as is.

## Navigation members

Besides providing the ability to execute requests and get the deserialized result, request builders also act as the foundation for the fluent style API. For this reason you'll find multiple types of members dedicated to this function.

### Properties

Properties are used for navigation path segments which declare no parameters.

In the following example:

```csharp
var requestBuilder = todoClient.TaskLists["taskListId"].Todos["todoId"].AssignedTo;
```

Two navigation properties are in use in this fluent style API call: `.TaskLists` and `.Todos`

The underlying URL template looks like `{+baseurl}/taskLists/{task_list_id}/todos/{todo_id}/assignedTo{?select,expand}`, where both the `taskLists` and `todos` path segments do not require any parameter.

> **Note:** If the target language doesn't support auto-property getters (get keyword in C# or TypeScript) a method with no parameter will be used instead.

This property always return a value (nonnull) and never throws an exception.

### Indexers

Indexers are used for navigation path segments which declare a single unnamed parameter. This design choice was made as often APIs leverage that pattern for indexable collections endpoints.

In the following example:

```csharp
var requestBuilder = todoClient.TaskLists["taskListId"].Todos["todoId"].AssignedTo;
```

Two indexers are used where brackets are present.

The underlying URL template looks like `{+baseurl}/taskLists/{task_list_id}/todos/{todo_id}/assignedTo{?select,expand}`, where both the `{task_list_id}` and `{todo_id}` path segments are single unnamed path parameters.

It is important to note that even if an endpoint to index in the collection `{+baseurl}/taskLists/{task_list_id}` does not exist, it will not impact the generation of the fluent style API for the sub-endpoint. If that indexing endpoint does exist, the only impact will be to the parent request builder which will now expose a `Get` method.

> **Note:** for languages that do not support indexers, a method with a suffix and a single parameters is used instead.

```Java
var requestBuilder = todoClient.taskListsById("taskListId").todosById("todoId").assignedTo;
```

This indexer always return a value (nonnull) and never throws an exception.

### Method with parameters

Methods with parameters are used when path segments contain named parameters or multiple parameters.

In the following example:

```csharp
var requestBuilder = todoClient.TaskLists["taskListId"].GetReminders("startDate", "endDate");
```

One method with two parameters is present.

The underlying URL template looks like `{+baseurl}/taskLists/{task_list_id}/getReminders(startDate='{startDate}',endDate='{endDate}')/{?select,expand}`, when the `getReminders` path segment contains two named parameters `startDate` and `endDate`.

This method with parameters always return a value (nonnull) and never throw an exception.

## Request building and execution

Once the request path is built using the fluent style API, request builders provide methods to generate and execute requests.

### Request generators

Request generator methods follow this `Create{HTTPMethodName}RequestInformation` naming convention. And accept the following parameters:

- body: the request body if supported.
- query parameters: an object or callback to set the request query parameters.
- headers: an object or callback to set the request headers.
- options: a list of request options to set for the request. These options are generally read by the middleware pipeline.

These method will return an abstract request information object, that can be passed to the request adapter for execution, or use with other tasks (e.g. batching of requests).

### Request executors

Request executor methods follow this `{HTTPMethodName}{Suffix}` naming convention, where a suffix is applied when it makes sense for the target language (e.g. GetAsync in C#). These methods accept the same parameter set as the request generators as they call into these methods to generate the request and then pass the request information to the request adapter for execution. They provide an additional parameter:

- responseHandler: this parameter may be used to bypass the standard response handling for the current request and access the native HTTP response object.
