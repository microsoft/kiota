# kiota client generate

Now that we have a `kiota.config` file, all the parameters required to generate the code are stored in the file. The `kiota generate` command will read the `kiota.config` file and generate the code for each of the clients. 

It's also possible to specify for which API and client the code should be generated. This is useful when a project contains multiple APIs and clients. The `kiota generate --api-name "MyAPI" --client-name "MyClient"` command will read the `kiota.config` file and generate the code for the specified API and client. If it can't find the specified API or client, it will throw an error.

In scenarios where a developer only needs a single API or doesn't want to go through the ceremony of executing `kiota init`, it's possible to use `kiota generate` and initialize a `kiota.config` file with the values coming from the command parameters. No breaking changes are required to the existing `kiota generate` command.

#### kiota generate Parameters

> [!IMPORTANT]
> This list is only the new parameters that `kiota generate` should support.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--app-name \| --an` | No | My application | Name of the application |
| `--api-name \| --api` | No | graph | Name of the API |
| `--client-name \| --cn` | No | graphDelegated | Name of the client. Unique within the parent API. |

#### Using `kiota generate` with all parameters

```bash
kiota generate --app-name "My Application" --api-name "graph" --client-name "graphDelegated" --openapi "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml" --include-path "/me/chats#GET" --include-path "/me#GET" --language csharp --class-name "GraphClient" --namespace-name "Contoso.GraphApp" --backing-store --exclude-backward-compatible --serializer "Contoso.Json.CustomSerializer" --deserializer "Contoso.Json.CustomDeserializer" -structured-mime-types "application/json" --output "./generated/graph/csharp"
```

```javascript
{
  "name": "My application",
  "apis": {
    "graph": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://.../openapi.yaml",
      "includePatterns": ["/me/chats#GET", "/me#GET"],
      "excludePatterns": [],
      "clients": {
        "graphDelegated": {
          "language": "csharp",
          "outputPath": "./generated/graph/csharp",
          "clientClassName": "GraphClient",
          "clientNamespaceName": "Contoso.GraphApp",
          "features": {
            // Adding for future visibility, but not required for now
            /*"authentication": {
              "authenticationProvider": "Microsoft.Kiota.Authentication.AzureAuthProvider",
              "authenticationParameters": {
                "clientId": "guid"
              },
            },*/
            "serializers": [
              "Contoso.Json.CustomSerializer"
            ],
            "deserializers": [
              "Contoso.Json.CustomDeserializer"
            ],
            "structuredMimeTypes": [
              "application/json"
            ],
            "usesBackingStore": true,
            "includeAdditionalData": true
          }
        }
      }
    }
  }
}
```