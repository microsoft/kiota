# Welcome to the Kiota Authentication section

The Kiota Authentication libraries are language specific libraries implementing the authentication interfaces Kiota projects need once an SDK has been generated from an OpenAPI definition to authenticate before calling the API.
Your project will need a reference to the abstraction package to build and run, the following languages are currently supported:

- [Dotnet](https://github.com/microsoft/kiota-authentication-azure-dotnet): relies on [Azure identity](https://www.nuget.org/packages/Azure.Identity).
- [Go](https://github.com/microsoft/kiota-authentication-azure-go/): relies on [Azure identity](https://pkg.go.dev/github.com/Azure/azure-sdk-for-go/sdk/azidentity)
- [Java](https://github.com/microsoft/kiota-java/tree/main/components/authentication/azure) : relies on [Azure identity](https://docs.microsoft.com/en-us/java/api/overview/azure/identity-readme?view=azure-java-stable).
- [Ruby](https://github.com/microsoft/kiota-authentication-oauth-ruby/): relies on [OAuth2](https://rubygems.org/gems/oauth2)
- [TypeScript](https://github.com/microsoft/kiota-typescript/tree/main/packages/authentication/azure) : relies on [Azure identity](https://www.npmjs.com/package/@azure/identity).
- [Python](https://github.com/microsoft/kiota-authentication-azure-python) : relies on [Azure identity](https://pypi.org/project/azure-identity/).
- [PHP](https://github.com/microsoft/kiota-authentication-phpleague-php) : relies on [The PHP League OAuth 2 client](https://packagist.org/packages/league/oauth2-client) to authenticate against the Azure Identity platform.
