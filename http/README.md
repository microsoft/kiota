# Welcome to the Kiota Http section

The Kiota HTTP libraries are language specific libraries implementing the interfaces Kiota projects need once an SDK has been generated from an OpenAPI definition to execute requests.
Your project will need a reference to the abstraction package to build and run, the following languages are currently supported:

- [Dotnet](https://github.com/microsoft/kiota-http-dotnet): relies on [HttpClient](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=net-7.0).
- [Go](https://github.com/microsoft/kiota-http-go/): relies on [NetHttp](https://pkg.go.dev/net/http).
- [Java](https://github.com/microsoft/kiota-java/tree/main/components/http/okHttp) : relies on [OkHttp](https://square.github.io/okhttp/).
- [TypeScript](https://github.com/microsoft/kiota-typescript/tree/main/packages/http/fetch) : relies on [node-fetch](https://www.npmjs.com/package/cross-fetch) for node users and global browser fetch for browser applications.
- [PHP](https://github.com/microsoft/kiota-http-guzzle-php) : relies on [Guzzle](http://guzzlephp.org/)
- [Python](https://github.com/microsoft/kiota-http-python) : relies on [Httpx](https://www.python-httpx.org/).
