# Welcome to the Kiota Http section

The Kiota HTTP libraries are language specific libraries implementing the interfaces Kiota projects need once an SDK has been generated from an OpenAPI definition to execute requests.
Your project will need a reference to the abstraction package to build and run, the following languages are currently supported:

- [Dotnet](./dotnet/httpclient): relies on [HttpClient](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=net-6.0).
- [Go](./do/nethttp): relies on [NetHttp](https://pkg.go.dev/net/http).
- [Java](./java/okhttp) : relies on [OkHttp](https://square.github.io/okhttp/).
- [TypeScript](./typescript/fetch) : relies on [cross-fetch](https://www.npmjs.com/package/cross-fetch).
- [PHP](./php/guzzle) : relies on [Guzzle](http://guzzlephp.org/)
