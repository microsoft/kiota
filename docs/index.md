# Welcome to Kiota

Kiota is an OpenAPI based code generator for creating SDKs for HTTP APIs. The goal is to produce a lightweight, low maintenance, code generator that is fast enough to run as part of the compile time tool-chain but scalable enough to handle the largest APIs.

Current SDK tooling assumes that consumers of the API want to consume API resources using the same boundaries as the teams that provide the APIs. However, that is often not the case.  Many companies are beginning to use API Management gateways and portals to bring APIs across their organization together and provide a coherent and consistent experience across many APIs.  However, SDKs continue to be shipped based on the team that provided the API.  HTTP URI spaces allow for seamless integration of many APIs into a coherent whole. SDK tooling should enable the same level of integration.

### Goals

- Fast and scalable source code generator to simplify calling HTTP APIs
- Leverage the full capabilities of OpenAPI descriptions
- Enable low effort implmentation of new language support
- Generate only the source code necessary by building on core library
- Minimize external dependencies
- Leverage JSON Schema descriptions to generate primitive based model serizalization/deserializtion code
- Enable generation of code for only a specified subset of an OpenAPI description
- Generate code that enables IDE autocomplete to aid in API resource discovery
- Enable full access to HTTP capabilities
- Lightweight, easy to install command line tool

### Non-Goals

- Extensibility model for creating different SDK API shapes
- Support for other API description formats

### API Style

Basic read and write syntax for a resource.

```csharp

var client = new ApiClient();
var user = await client.Users["bob@contoso.com"].GetAsync();

var newUser = new User() {
    FirstName = "Bill",
    LastName = "Brown"
}

await client.Users.PostAsync(newUser)

```

Resources are accessed via relation properties starting from the client object.  Collections of resources can be accessed by an indexer and a parameter. Once the desired resource has been referenced, the supported HTTP methods are exposed by corresponding methors.  Deeply nested resource hierarchy can be accessed by continuing to traverse relationships.

```csharp

var client = new ApiClient();
var message = await client.Users["bob@contoso.com"]
                          .MailFolders["Inbox"]
                          .Messages[23242]
                          .GetAsync();

```

The client object is a "request builder" object, and forms the root of a hierarchy of request builder objects that can access any number of APIs that are merged into a common URI space.

Requests can be further refined by providing query parameters. Each HTTP operation method that supports query parameters accepts a lambda that can configure an object with the desired query parameters.

```csharp

var client = new ApiClient();
var message = await client.Users["bob@contoso.com"]
                          .Events
                          .GetAsync(q => {  q.StartDateTime = DateTime.Now;
                                            q.EndDateTime = DateTime.Now.AddDays(7);
                                        });

```

Using a configured query parameter object prevents tight coupling on the order of query parameters and make optional parameters easy to implement across languages.

For more information on the various components used by the Kiota SDK, see the following topics:

- [Request Builders](requestbuilders)
- [Models](models)
- [Core Library](corelibrary)