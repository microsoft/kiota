# kiota client add

## Description 

`kiota client add` allows a developer to add a new API client to the `kiota.config` file. If no `kiota.config` file is found, a new `kiota.config` file would be created. The command will add a new entry to the `clients` section of the `kiota.config` file. Once this is done, a local copy of the OpenAPI description is generated and kepts in the .

When executing, a new API entry will be added and will use the `--client-name` parameter as the key for the map. When loading the OpenAPI description, it will generate a hash of the description to enable change detection of the description and save it as part of the `descriptionHash` property. It will also store the location of the description in the `descriptionLocation` property. If `--include-path` or `--exclude-path` are provided, they will be stored in the `includePatterns` and `excludePatterns` properties respectively.

Every time an API client is added, a copy of the OpenAPI description file will be stored in the `./.kiota` folder. The file will be named using the hash of the description. This will allow the CLI to detect changes in the description and avoid downloading the description again if it hasn't changed. 

At the same time, an [API Manifest](https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html#section-2.5-3) file will be generated (if non existing) or edited (if already existing) to represent the surface of the API being used. This file will be named `apimanifest.json` and will be stored in the `./.kiota` folder. This file will be used to generate the code files.

Once the `kiota.config` file is generated, the OpenAPI description file is saved locally and the API Manifest is available, the code generation will be executed.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--client-name \| --cn` | Yes | graphDelegated | Name of the client. Unique within the parent API. If not provided, defaults to --class-name or its default. |
| `--openapi \| -d` | Yes | https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml | The location of the OpenAPI description in JSON or YAML format to use to generate the SDK. Accepts a URL or a local path. |
| `--include-path \| -i` | No | /me/chats#GET | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. |
| `--exclude-path \| -e` | No | \*\*/users/\*\* | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. |
| `--language \| -l` | Yes | csharp | The target language for the generated code files or for the information. |
| `--class-name \| -c` | No | GraphClient | The name of the client class. Defaults to `Client`. |
| `--namespace-name \| -n` | No | Contoso.GraphApp | The namespace of the client class. Defaults to `Microsoft.Graph`. |
| `--backing-store \| -b` | No | | Defaults to `false` |
| `--exclude-backward-compatible \| --ebc` | No |  | Whether to exclude the code generated only for backward compatibility reasons or not. Defaults to `false`. |
| `--serializer \| -s` | No | `Contoso.Json.CustomSerializer` | One or more module names that implements ISerializationWriterFactory. Default are documented [here](https://learn.microsoft.com/openapi/kiota/using#--serializer--s). |
| `--deserializer \| --ds` | No | `Contoso.Json.CustomDeserializer` | One or more module names that implements IParseNodeFactory. Default are documented [here](https://learn.microsoft.com/en-us/openapi/kiota/using#--deserializer---ds). |
| `--structured-mime-types \| -m` | No | `application/json` |Any valid MIME type which will match a request body type or a response type in the OpenAPI description. Default are documented [here](https://learn.microsoft.com/en-us/openapi/kiota/using#--structured-mime-types--m). |
| `--output \| -o` | No | ./generated/graph/csharp | The output directory or file path for the generated code files. Defaults to `./output`. |

> [!NOTE] 
> It is not required to use the CLI to add new clients. It is possible to add a new client by adding a new entry in the `clients` section of the `kiota.config` file. See the [kiota.config schema](../schemas/kiota.config.md) for more information.

## Using `kiota client add`

```bash
kiota client add --client-name "graphDelegated" --openapi "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml" --include-path "**/users/**" --language csharp --class-name "GraphClient" --namespace-name "Contoso.GraphApp" --backing-store --exclude-backward-compatible --serializer "Contoso.Json.CustomSerializer" --deserializer "Contoso.Json.CustomDeserializer" -structured-mime-types "application/json" --output "./generated/graph/csharp"
```

```json
"clients": {
  "graphDelegated": {
    "descriptionHash": "9EDF8506CB74FE44...",
    "descriptionLocation": "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml",
    "includePatterns": ["**/users/**"],
    "excludePatterns": [],
    "language": "csharp",
    "outputPath": "./generated/graph/csharp",
    "clientClassName": "GraphClient",
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
```

_The resulting `kiota.config` file will look like this:_

```json
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
      "clientClassName": "GraphClient",
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

```json
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

## File structure
```bash
/
 └─.kiota
    └─kiota.config
    └─apimanifest.json
    └─definitions
       └─9EDF8506CB74FE44.yaml
 └─generated
    └─graph
       └─csharp
          └─... # Generated code files
          └─GraphClient.cs
```

## Open Questions

- [ ] How do we determine the `name` and `contactEmail` of the `publisher` in the API Manifest? kiota config --global?
- [ ] Can we automatically generate all `authorizationRequirements` for the endpoints selected or these are left to the developers to add?