---
parent: Get started
---

# Build SDKs for CLI

## Required tools
A commandline tool is required. We recommend:
- [Windows Terminal + version](https://apps.microsoft.com/store/detail/windows-terminal/9N0DX20HK701?hl=en-us&gl=us)
- [.NET SDK 7.0](https://get.dot.net/7)
- A commandline that supports Bash?
- MacOS terminal

## Target project requirements
Before you can compile and run the generated files, you will need to make sure they are part of a project with the required dependencies. After creating a new project, or reusing an existing one, you will need to add references to the [abstraction](https://github.com/microsoft/kiota-abstractions-dotnet), [authentication](https://github.com/microsoft/kiota-authentication-azure-dotnet), [cli-commons](https://github.com/microsoft/kiota-cli-commons), [http](https://github.com/microsoft/kiota-http-dotnet), and [FORM](https://github.com/microsoft/kiota-serialization-form-dotnet), [JSON](https://github.com/microsoft/kiota-serialization-json-dotnet) and [text](https://github.com/microsoft/kiota-serialization-text-dotnet) serialization packages from the NuGet feed.

## Creating target projects

> **Note:** you can use an existing project if you have one, in that case, you can skip the following section.

Execute the following command in the directory you want to create a new project.

```bash
dotnet new console -o GetUserClient
dotnet new gitignore
```

## Adding dependencies

```bash
dotnet add package Microsoft.Kiota.Abstractions --prerelease
dotnet add package Microsoft.Kiota.Cli.Commons --prerelease
dotnet add package Microsoft.Kiota.Http.HttpClientLibrary --prerelease
dotnet add package Microsoft.Kiota.Serialization.Form --prerelease
dotnet add package Microsoft.Kiota.Serialization.Json --prerelease
dotnet add package Microsoft.Kiota.Serialization.Text --prerelease
dotnet add package Microsoft.Kiota.Authentication.Azure --prerelease
dotnet add package Azure.Identity
```

### Getting access to the packages

### Add Kiota and dependencies

> **Note:** Find current version numbers for Kiota packages at [Nexus Repository Manager](https://oss.sonatype.org/).


## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```bash
kiota generate --openapi getme.yml --language shell -c GetUserApiClient -n GetUserClient.ApiClient -o ./Client
```

## Registering an application in Azure AD

## Creating an application registration

> **Note:** this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

Follow the instructions in [Register an application for Microsoft identity platform authentication](register-app.md) to get an application ID (also know as a client ID).

The final step is to update the **Program.cs** file that was generated as part of the console application to include the code below. Replace `YOUR_CLIENT_ID` with the client ID from your app registration.

```csharp
using Azure.Identity;
using GetUserClient.ApiClient;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Cli.Commons.Extensions;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace GetUserClient;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new GetUserApiClient().BuildRootCommand();
        rootCommand.Description = "CLI description";

        // Set up services
        var builder = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseRequestAdapter(ic =>
                {
                    var clientId = "YOUR_CLIENT_ID";
                    var clientSecret = "YOUR_CLIENT_SECRET"
                    var tenantId = "YOUR_TENANT_ID";

                    // The auth provider will only authorize requests to
                    // the allowed hosts, in this case Microsoft Graph
                    var allowedHosts = new [] { "graph.microsoft.com" };
                    var graphScopes = new [] { "User.Read" };
                    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

                    var authProvider = new AzureIdentityAuthenticationProvider(credential, allowedHosts, scopes: graphScopes);
                    var adapter = new HttpClientRequestAdapter(authProvider);
                    adapter.BaseUrl = "https://graph.microsoft.com/v1.0";
                    return adapter;
                }).RegisterCommonServices();
        
        return await builder.Build().InvokeAsync(args);
    }
}

```

> **Note:**
>
> - If the target API doesn't require any authentication, you can use the **AnonymousAuthenticationProvider** instead.
> - If the target API relies on an API key for authentication, you can use the **ApiKeyAuthenticationProvider** instead.
> - If the target API requires an `Authorization bearer <token>` header but doesn't rely on the Microsoft identity platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.
> - If the target API requires any other form of authentication schemes, you can implement the **IAuthenticationProvider** interface.


When ready to execute the application, execute the following command in your project directory.

```bash
dotnet run -- me get
```

### Samples
You can find additional samples here: [CLI SDK samples](https://github.com/microsoftgraph/msgraph-cli/tree/main/samples)


## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
