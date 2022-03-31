# Welcome to the Kiota Http section

The Kiota HTTP libraries are language specific libraries implementing the interfaces Kiota projects need once an SDK has been generated from an OpenAPI definition to execute requests.
Your project will need a reference to the abstraction package to build and run, the following languages are currently supported:

- [Dotnet](https://github.com/microsoft/kiota-http-dotnet): relies on [HttpClient](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=net-6.0).
- [Go](./do/nethttp): relies on [NetHttp](https://pkg.go.dev/net/http).
- [Java](./java/okhttp) : relies on [OkHttp](https://square.github.io/okhttp/).
- [TypeScript](https://github.com/microsoft/kiota-typescript/tree/main/packages/http/fetch : relies on [node-fetch](https://www.npmjs.com/package/cross-fetch) for node users and global browser fetch for browser applications.
- [PHP](./php/guzzle) : relies on [Guzzle](http://guzzlephp.org/)
