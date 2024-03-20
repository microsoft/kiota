# kiota client add

## Description 

`kiota client add` allows a developer to add a new API client to the `workspace.json` file. If no `workspace.json` file is found, a new `workspace.json` file would be created in the `.kiota` directory in current working directory. The command will add a new entry to the `clients` section of the `workspace.json` file. Once this is done, a local copy of the OpenAPI description is generated and kept in the `.kiota/clients` folder. If a client with the same name already exists, the command will fail and display an actionable error message.

When executing, a new API entry will be added and will use the `--client-name` parameter as the key for the map. When loading the OpenAPI description, it will store the location of the description in the `descriptionLocation` property. If `--include-path` or `--exclude-path` are provided, they will be stored in the `includePatterns` and `excludePatterns` properties respectively.

Every time an API client is added, a copy of the OpenAPI description file will be stored in the `./.kiota/clients/{client-name}.yaml|json` folder. The files will be named using the API client name. This will allow the CLI to detect changes in the description and avoid downloading the description again if it hasn't changed. 

At the same time, an [API Manifest](https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html) file will be generated (if non existing) or edited (if already existing) in the `.kiota` folder next to `workspace.json`. API Manifest represents a snapshot of API dependencies and permissions required to access those APIs. This file will represent a concatenated surface of all APIs used across plugins and clients. Both files, `apimanifest.json` and `workspace.json` will be used to generate the code files. A new hash composed of the Kiota version, the OpenAPI description location and the properties of the manifest will be generated and would trigger an update to the [API Manifest][https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html].

Once the `workspace.json` file is generated and the OpenAPI description file is saved locally, the code generation will be executed and then the API Manifest would become available.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `--client-name \| --cn` | Yes | graphDelegated | Name of the client and the client class. Unique within the parent API. Defaults to `Client` | No |
| `--openapi \| -d` | Yes | https://aka.ms/graph/v1.0/openapi.yaml | The location of the OpenAPI description in JSON or YAML format to use to generate the SDK. Accepts a URL or a local path. | No |
| `--search-key \| --sk` | No | github::microsoftgraph/msgraph-metadata/graph.microsoft.com/v1.0 | The search key used to locate the OpenAPI description. | Yes, without its value |
| `--include-path \| -i` | No | /me/chats#GET | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. | Yes, without its value |
| `--exclude-path \| -e` | No | \*\*/users/\*\* | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. | Yes, without its value |
| `--language \| -l` | Yes | csharp | The target language for the generated code files or for the information. | Yes |
| `--namespace-name \| -n` | No | Contoso.GraphApp | The namespace of the client class. Defaults to `Microsoft.Graph`. | Yes, without its value |
| `--backing-store \| -b` | No | | Defaults to `false` | Yes |
| `--exclude-backward-compatible \| --ebc` | No |  | Whether to exclude the code generated only for backward compatibility reasons or not. Defaults to `false`. | Yes |
| `--structured-media-types \| -m` | No | `application/json` | Any valid media type which will match a request body type or a response type in the OpenAPI description. Default are documented [here](https://learn.microsoft.com/en-us/openapi/kiota/using#--structured-mime-types--m). | Yes |
| `--skip-generation \| --sg` | No | true | When specified, the generation would be skipped. Defaults to false. |Yes |
| `--output \| -o` | No | ./generated/graph/csharp | The output directory or file path for the generated code files. This is relative to the location of `workspace.json`. Defaults to `./output`. | Yes, without its value |

> [!NOTE] 
> It is not required to use the CLI to add new clients. It is possible to add a new client by adding a new entry in the `clients` section of the `workspace.json` file. See the [workspace.json schema](../schemas/workspace.json) for more information. Using `kiota client generate --client-name myClient` would be required to generate the code files.

## Telemetry

## Using `kiota client add`

```bash
kiota client add --client-name "GraphClient" --openapi "https://aka.ms/graph/v1.0/openapi.yaml" --include-path "**/users/**" --language csharp --namespace-name "Contoso.GraphApp" --backing-store --exclude-backward-compatible --serializer "Contoso.Json.CustomSerializer" --deserializer "Contoso.Json.CustomDeserializer" -structured-mime-types "application/json" --output "./generated/graph/csharp"
```

_The resulting `workspace.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
  "clients": {
    "GraphClient": {
      "descriptionLocation": "https://aka.ms/graph/v1.0/openapi.yaml",
      "includePatterns": ["**/users/**"],
      "excludePatterns": [],
      "language": "csharp",
      "outputPath": "./generated/graph/csharp",
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
    "GraphClient": {
      "x-ms-kiotaHash": "9EDF8506CB74FE44...",
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

## File structure

```bash
/
 └─.kiota
    └─clients
       └─GraphClient
         └─description.yaml
    └─apimanifest.json
    └─workspace.json 
 └─generated
    └─graph
       └─csharp
          └─... # Generated code files
          └─GraphClient.cs       
```
