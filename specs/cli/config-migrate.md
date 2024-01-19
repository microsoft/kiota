#  `kiota config migrate`

This command is valuable in cases where a code base was created with Kiota v1.0 and needs to be migrated to the latest version of Kiota. The `kiota config migrate` command will identify and locate the closest `kiota-config.json` file available. If a file can't be found, it would initialize a new `kiota-config.json` file. Then, it would identify all `kiota-lock.json` files that are within this folder structure and add each of them to the `kiota-config.json` file. Adding the clients to the `kiota-config.json` file would not trigger the generation as it only affects the `kiota-config.json` file. The `kiota client generate` command would need to be executed to generate the code for the clients.

In the case where conflicting API client names would be migrated, the command will error out and invite the user to re-run the command providing more context for the `--client-name` parameter.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
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
      "descriptionLocation": "https://aka.ms/graph/v1.0/openapi.yaml",
      "includePatterns": ["**/users/**"],
      "excludePatterns": [],
      "language": "csharp",
      "outputPath": "./generated/graph/csharp",
      "clientClassName": "GraphServiceClient",
      "clientNamespaceName": "Contoso.GraphApp",
      "structuredMediaTypes": [
          "application/json"
      ],
      "usesBackingStore": true,
      "includeAdditionalData": true
    },
    "pythonGraphServiceClient": {
      "descriptionLocation": "https://aka.ms/graph/v1.0/openapi.yaml",
      "includePatterns": ["**/users/**"],
      "excludePatterns": [],
      "language": "python",
      "outputPath": "./generated/graph/python",
      "clientClassName": "GraphServiceClient",
      "clientNamespaceName": "Contoso.GraphApp",
      "structuredMediaTypes": [
          "application/json"
      ],
      "usesBackingStore": true,
      "includeAdditionalData": true
    }
  }
}
```

_The resulting `apimanifest.json` file will look like this:_

```jsonc
{
  "apiDependencies": {
    "csharpGraphServiceClient": {
      "x-ms-kiotaHash": "9EDF8506CB74FE44...",
      "x-ms-kiotaVersion": "1.11.0",
      "apiDescriptionUrl": "https://aka.ms/graph/v1.0/openapi.yaml",
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
    },
    "pythonGraphServiceClient": {
      "x-ms-kiotaHash": "9EDF8506CB74FE44...",
      "x-ms-kiotaVersion": "1.11.0",
      "apiDescriptionUrl": "https://aka.ms/graph/v1.0/openapi.yaml",
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

_The resulting file structure will look like this:_

```bash
/
  └─.kiota
    └─definitions
       └─csharpGraphServiceClient.yaml
       └─pythonGraphServiceClient.yaml
  └─generated
    └─graph
       └─csharp
          └─... # Generated code files
          └─GraphClient.cs
        └─python
          └─... # Generated code files
          └─graph_client.py       
  └─apimanifest.json
  └─kiota-config.json 
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
      "descriptionLocation": "https://aka.ms/graph/v1.0/openapi.yaml",
      "includePatterns": ["**/users/**"],
      "excludePatterns": [],
      "language": "csharp",
      "outputPath": "./generated/graph/csharp",
      "clientClassName": "GraphServiceClient",
      "clientNamespaceName": "Contoso.GraphApp",
      "structuredMediaTypes": [
          "application/json"
      ],
      "usesBackingStore": true,
      "includeAdditionalData": true
    }
  }
}
```


```bash
/
  └─.kiota
    └─definitions
       └─graphDelegated.yaml
  └─generated
    └─graph
       └─csharp
          └─... # Generated code files
          └─GraphClient.cs
        └─python
          └─... # Generated code files
          └─graph_client.py   
          └─kiota-lock.json    
  └─apimanifest.json
  └─kiota-config.json 
```