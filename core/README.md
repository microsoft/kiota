# Welcome to the Kiota Core section

The Kiota Core libraries are language specific libraries implementing the basic constructs Kiota projects need once an SDK has been generated from an OpenAPI definition.
Your project will need a reference to the abstraction package to build and run, the following languages are currently supported:

- [Dotnet](./dotnet): relies on [HttpClient](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=net-5.0), [Azure identity](https://www.nuget.org/packages/Azure.Identity) for token acquisition for requests and [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=net-5.0) for JSON serialization/deserialization
- [Java](./java) : relies on [OkHttp](https://square.github.io/okhttp/), [Azure identity](https://docs.microsoft.com/en-us/java/api/overview/azure/identity-readme?view=azure-java-stable) for token acquisition for requests and [Gson](https://github.com/google/gson) for JSON serialization/deserialization.
- [TypeScript](./typescript) : relies on [cross-fetch](https://www.npmjs.com/package/cross-fetch) for request, [Azure identity](https://www.npmjs.com/package/@azure/identity) for token acquisition and the native JSON capabilities for JSON serialization/deserialization.
