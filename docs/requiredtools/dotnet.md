# Required tools for Dotnet

- [.NET SDK 5.0](https://dotnet.microsoft.com/download)

## Target project requirements

Before you can compile and run the generated files, you will need to make sure they are part of a project with the required dependencies. After creating a new project, or reusing an existing one, you will need to add references to the [abstraction](../../abstractions/dotnet) and the [authentication](../../authentication/dotnet/azure), [http](../../http/dotnet/httpclient), [serialization](../../serialization/dotnet/json) packages from the GitHub feed.

## Creating target projects

> Note: you can use an existing project if you have one, in that case, you can skip the following section.

Execute the following command in the directory you want to create a new project.

```Shell
dotnet new console
dotnet new gitignore
```

> Note: in this example the console template is used, but you can use any CSharp template.

## Adding dependencies

If you have not already, you will need to create a nuget.config to enable access to the packages in the GitHub package feed.  The article on installing the [Kiota command line](../generator/tool.md) tool shows how to do this.

Once the pacakge feed is accessible the following packages can be added to the project.

```Shell
dotnet add package Microsoft.Kiota.Abstractions
dotnet add package Microsoft.Kiota.Http.HttpClient
dotnet add package Microsoft.Kiota.Serialization.Json
dotnet add package Microsoft.Kiota.Authentication.Azure
dotnet add package Azure.Identity
```

Only the first package, Microsoft.Kiota.Abstractions is required. The other packages provide default implementations that you can choose to replace with your own implementations if you wish.

## Generating the SDK

Kiota generates SDKs from OpenAPI documents. The example below is a minimal OpenAPI description that describes how to call the `/me` endpoint on Microsoft Graph.

Create a file called openapi.yaml with the following contents:

```yaml
openapi: 3.0.3
info:
  title: app that calls graph and gets the me profile
  version: 1.0.0
paths:
  /me:
    get:
      responses:
        200: 
          description: Success!
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/microsoft.graph.user"
components:
  schemas:
    microsoft.graph.user:
      type: object
      properties:
        id:
          type: string
        displayName:
          type: string
```

You can then use the Kiota command line tool to generate the SDK classes.

```shell
kiota -d openapi.yml -o graphclient -n GraphClient
```

## Creating an application registration

> Note: this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

To be able to authenticate against the demo application against Microsoft Graph, you will need to create an application registration.  You can do this via the Azure portal, or if you have [Microsoft Graph PowerShell](https://www.powershellgallery.com/packages/Microsoft.Graph) installed, you can use the following command to create the application.

```PowerShell
$app = New-MgApplication   -displayName "NativeGraphApp" `
                            -IsFallbackPublicClient `
                            -PublicClient @{ `
                                RedirectUris = "http://localhost" `
                            }
```

Record the value of the ClientId property of the $app object as it will be needed in a later step.

## Creating the client application

The final step is to update the program.cs file that was generated as part of the console application to include the code below.

```CSharp
using System;
using System.Threading.Tasks;
using GraphClient;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Http.HttpClient;
using Azure.Identity;

namespace GraphApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var credential = new InteractiveBrowserCredential("<insert clientId from $app.ClientId>");
            var authProvider = new AzureIdentityAuthenticationProvider(credential, new string[] {"User.Read"});
            var core = new HttpCore(authProvider);
            var apiClient = new ApiClient(core);
            var me = await apiClient.Me.GetAsync();
            Console.WriteLine(me.DisplayName);
        }
    }
}
```

> Note: if the target API doesn't require any authentication, you can use the **AnonymousAuthenticationProvider** instead.  
> Note: if the target API requires a Authorization bearer \<token> header but doesn't rely on the Microsoft Identity Platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.  
> Note: if the target API requires any other form of authentication schemes, you can implement the **IAuthenticationProvider** interface.
