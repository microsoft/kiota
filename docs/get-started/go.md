---
parent: Get started
---

# Build SDKs for Go

## Required tools

- [Go 1.17](https://golang.org/dl/)

## Target project requirements

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](https://github.com/microsoft/kiota/tree/main/abstractions/go/), [authentication](https://github.com/microsoft/kiota/tree/main/authentication/go/azure), [http](https://github.com/microsoft/kiota/tree/main/http/go/http/), and [serialization](https://github.com/microsoft/kiota/tree/main/serialization/go/json/) packages from the GitHub feed.

## Creating target projects

> **Note:** you can use an existing project if you have one, in that case, you can skip the following section.

Execute the following commands in the directory where you want to create a new project.

```bash
go mod init getuser
```

## Adding dependencies

Create a file named **getuser.go** and add the following code.

```bash
go get github.com/microsoft/kiota/abstractions/go
go get github.com/microsoft/kiota/http/go/nethttp
go get github.com/microsoft/kiota/serialization/go/json
go get github.com/microsoft/kiota/authentication/go/azure
go get github.com/Azure/azure-sdk-for-go/sdk/azidentity
```

Only the first package, `github.com/microsoft/kiota/abstractions/go`, is required. The other packages provide default implementations that you can choose to replace with your own implementations if you wish.

## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```shell
kiota -l go -d ../getme.yml -c GraphApiClient -n getuser/client -o ./client
```

## Creating an application registration

> **Note:** this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

Follow the instructions in [Register an application for Microsoft identity platform authentication](register-app.md) to get an application ID (also know as a client ID).

## Creating the client application

Create a file in the root of the project named **getuser.go** and add the following code. Replace `YOUR_CLIENT_ID` with the client ID from your app registration.

```go
package main

import (
  "context"
  "fmt"

  "getuser/client"

  "github.com/Azure/azure-sdk-for-go/sdk/azidentity"
  azure "github.com/microsoft/kiota/authentication/go/azure"
  http "github.com/microsoft/kiota/http/go/nethttp"
)

func main() {
  clientId := "YOUR_CLIENT_ID"

  // The auth provider will only authorize requests to
  // the allowed hosts, in this case Microsoft Graph
  allowedHosts := []string{"graph.microsoft.com"}
  graphScopes := []string{"User.Read"}

  credential, err := azidentity.NewDeviceCodeCredential(&azidentity.DeviceCodeCredentialOptions{
    ClientID: clientId,
    UserPrompt: func(ctx context.Context, dcm azidentity.DeviceCodeMessage) error {
      fmt.Println(dcm.Message)
      return nil
    },
  })

  if err != nil {
    fmt.Printf("Error creating credential: %v\n", err)
  }

  authProvider, err := azure.NewAzureIdentityAuthenticationProviderWithScopesAndValidHosts(
    credential, graphScopes, allowedHosts)

  if err != nil {
    fmt.Printf("Error creating auth provider: %v\n", err)
  }

  adapter, err := http.NewNetHttpRequestAdapter(authProvider)

  if err != nil {
    fmt.Printf("Error creating request adapter: %v\n", err)
  }

  client := client.NewGraphApiClient(adapter)

  me, err := client.Me().Get(nil)

  if err != nil {
    fmt.Printf("Error getting user: %v\n", err)
  }

  fmt.Printf("Hello %s, your ID is %s\n", *me.GetDisplayName(), *me.GetId())
}
```

> **Note:**
>
> - If the target API doesn't require any authentication, you can use the **AnonymousAuthenticationProvider** instead.
> - If the target API requires an `Authorization: Bearer <token>` header but doesn't rely on the Microsoft Identity Platform, you can implement your own authentication provider by inheriting from **BaseBearerTokenAuthenticationProvider**.
> - If the target API requires any other form of authentication schemes, you can implement the **AuthenticationProvider** interface.

## Executing the application

When ready to execute the application, execute the following command in your project directory.

```shell
go run .
```

## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started/go) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
