---
parent: Get started
---

# Build SDKs for .NET

## Required tools

- [.NET SDK 7.0](https://get.dot.net/7)

## Target project requirements

Before you can compile and run the generated files, you will need to make sure they are part of a project with the required dependencies. After creating a new project, or reusing an existing one, you will need to add references to the [abstraction](https://github.com/microsoft/kiota-abstractions-dotnet), [authentication](https://github.com/microsoft/kiota-authentication-azure-dotnet), [http](https://github.com/microsoft/kiota-http-dotnet), and [FORM](https://github.com/microsoft/kiota-serialization-form-dotnet), [JSON](https://github.com/microsoft/kiota-serialization-json-dotnet) and [text](https://github.com/microsoft/kiota-serialization-text-dotnet) serialization packages from the GitHub feed.

## Creating target projects

> **Note:** you can use an existing project if you have one, in that case, you can skip the following section.

Execute the following command in the directory you want to create a new project.

```bash
dotnet new console -o GetUserClient
dotnet new gitignore
```

> **Note:** in this example the console template is used, but you can use any C# template.

## Adding dependencies

```bash
dotnet add package Microsoft.Kiota.Abstractions --prerelease
dotnet add package Microsoft.Kiota.Http.HttpClientLibrary --prerelease
dotnet add package Microsoft.Kiota.Serialization.Form --prerelease
dotnet add package Microsoft.Kiota.Serialization.Json --prerelease
dotnet add package Microsoft.Kiota.Serialization.Text --prerelease
dotnet add package Microsoft.Kiota.Authentication.Azure --prerelease
dotnet add package Azure.Identity
```

Only the first package, `Microsoft.Kiota.Abstractions` is required. The other packages provide default implementations that you can choose to replace with your own implementations if you wish.

## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```bash
kiota generate -d getme.yml -c GetUserApiClient -n GetUserClient.ApiClient -o ./Client
```

## Creating an application registration

> **Note:** this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

Follow the instructions in [Register an application for Microsoft identity platform authentication](register-app.md) to get an application ID (also know as a client ID).

## Creating the client application

The final step is to update the **Program.cs** file that was generated as part of the console application to include the code below. Replace `YOUR_CLIENT_ID` with the client ID from your app registration.

```csharp
using Azure.Identity;
using GetUserClient.ApiClient;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Http.HttpClientLibrary;

var clientId = "YOUR_CLIENT_ID";

// The auth provider will only authorize requests to
// the allowed hosts, in this case Microsoft Graph
var allowedHosts = new [] { "graph.microsoft.com" };
var graphScopes = new [] { "User.Read" };

var credential = new DeviceCodeCredential((code, cancellation) =>
{
    Console.WriteLine(code.Message);
    return Task.FromResult(0);
},
clientId);

var authProvider = new AzureIdentityAuthenticationProvider(credential, allowedHosts, scopes: graphScopes);
var requestAdapter = new HttpClientRequestAdapter(authProvider);
var client = new GetUserApiClient(requestAdapter);

var me = await client.Me.GetAsync();
Console.WriteLine($"Hello {me.DisplayName}, your ID is {me.Id}");

```

> **Note:**
>
> - If the target API doesn't require any authentication, you can use the **AnonymousAuthenticationProvider** instead.
> - If the target API relies on an API key for authentication, you can use the **ApiKeyAuthenticationProvider** instead.
> - If the target API requires an `Authorization bearer <token>` header but doesn't rely on the Microsoft identity platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.
> - If the target API requires any other form of authentication schemes, you can implement the **IAuthenticationProvider** interface.

## Executing the application

When ready to execute the application, execute the following command in your project directory.

```bash
dotnet run
```

## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started/dotnet) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
