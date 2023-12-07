#  `kiota config migrate`

This command is valuable in cases where a code base was created with Kiota v1.0 and needs to be migrated to the latest version of Kiota. The `kiota config migrate` command will identify and locate the closest `kiota-config.json` file available. If a file can't be found, it would initialize a new `kiota-config.json` file. Then, it would identify all `kiota-lock.json` files that are within this folder structure and add each of them to the `kiota-config.json` file. Adding the clients to the `kiota-config.json` file would not trigger the generation as it only affects the `kiota-config.json` file. The `kiota client generate` command would need to be executed to generate the code for the clients.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--config-location \| --cl` | No | ./.kiota/kiota-config.json | A location where to find or create the `kiota-config.json` file. When not specified it will find an ancestor `kiota-config.json` file and if not found, will use `./.kiota/kiota-config.json`. |
| `--lock-location \| --ll` | No | ./output/pythonClient/kiota-lock.json | Location of the `kiota-lock.json` file. If not specified, all `kiota-lock.json` files within in the current directory tree will be used. |
| `--client-name \| --cn` | No | graphDelegated | Used with `--lock-location`, it would allow to specify a name for the API client. Else, name is auto-generated as a concatenation of the `language` and `clientClassName`. |

## Using `kiota config migrate`

Assuming the following folder structure:
```bash
/
  └─generated
    └─graph
       └─csharp
          └─... # Generated code files
          └─GraphClient.cs
          └─kiota-lock.json
        └─python
          └─... # Generated code files
          └─graph_client.py
          └─kiota-lock.json
```

```bash
kiota config migrate
```

_The resulting `kiota-config.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
  "clients": {
    "csharpGraphServiceClient": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml",
      "includePatterns": ["**/users/**"],
      "excludePatterns": [],
      "language": "csharp",
      "outputPath": "./generated/graph/csharp",
      "clientClassName": "GraphServiceClient",
      "clientNamespaceName": "Contoso.GraphApp",
      "features": {
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
    },
    "pythonGraphServiceClient": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml",
      "includePatterns": ["**/users/**"],
      "excludePatterns": [],
      "language": "python",
      "outputPath": "./generated/graph/python",
      "clientClassName": "GraphServiceClient",
      "clientNamespaceName": "Contoso.GraphApp",
      "features": {
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
```

_The resulting `apimanifest.json` file will look like this:_

```jsonc
{
  "publisher": {
    "name": "Microsoft Graph",
    "contactEmail": "graphsdkpub@microsoft.com"
  },
  "apiDependencies": {
    "graphDelegated": {
      "apiDescriptionUrl": "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml",
      "apiDeploymentBaseUrl": "https://graph.microsoft.com",
      "apiDescriptionVersion": "v1.0",
      "requests": [
        {
          "method": "GET",
          "uriTemplate": "/users"
        },
        {
          "method": "POST",
          "uriTemplate": "/users"
        },
        {
          "method": "GET",
          "uriTemplate": "/users/$count"
        },
        {
          "method": "GET",
          "uriTemplate": "/users/{user-id}"
        },
        {
          "method": "PATCH",
          "uriTemplate": "/users/{user-id}"
        },
        {
          "method": "DELETE",
          "uriTemplate": "/users/{user-id}"
        }
      ]
    }
  }
}
```

## Using `kiota config migrate` for a specific `kiota-lock.json` file and a specific client name

Assuming the following folder structure:
```bash
/
  └─generated
    └─graph
       └─csharp
          └─... # Generated code files
          └─GraphClient.cs
          └─kiota-lock.json
        └─python
          └─... # Generated code files
          └─graph_client.py
          └─kiota-lock.json
```

```bash
kiota config migrate --lock-location ./generated/graph/csharp/kiota-lock.json --client-name graphDelegated
```

_The resulting `kiota-config.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
  "clients": {
    "graphDelegated": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml",
      "includePatterns": ["**/users/**"],
      "excludePatterns": [],
      "language": "csharp",
      "outputPath": "./generated/graph/csharp",
      "clientClassName": "GraphServiceClient",
      "clientNamespaceName": "Contoso.GraphApp",
      "features": {
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