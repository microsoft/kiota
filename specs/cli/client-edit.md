# kiota client edit

## Description 

`kiota client update` allows a developer to edit an existing API client int the `kiota-config.json` file. If either the `kiota-config.json` file or if the `--client-name` client can't be found within the `kiota-config.json` file, the command should error out and let the developer know.

When executing, the API entry defined by the `--client-name` parameter will be modified. All parameters should be supported and the only required one is `--client-name`. All others are optional as they would only modify the configuration of the API client. If the OpenAPI description location changed or any properties of the client entry in `kiota-config.json`, a new hash composed of the Kiota version, the OpenAPI description location and the properties of the client will be generated and and would trigger an update to the [API Manifest](https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html#section-2.5-3).

Once the `kiota-config.json` file and the API Manifest are updated, the code generation will be executed based on the newly updated API client configuration.

## Parameters

| Parameters | Required | Example | Description | Telemetry | 
| -- | -- | -- | -- | -- |
| `--client-name \| --cn` | Yes | graphDelegated | Name of the client. Unique within the parent API. If not provided, defaults to --class-name or its default. | Yes, without its value |
| `--openapi \| -d` | No | https://aka.ms/graph/v1.0/openapi.yaml | The location of the OpenAPI description in JSON or YAML format to use to generate the SDK. Accepts a URL or a local path. | Yes, without its value |
| `--include-path \| -i` | No | /me/chats#GET | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. | Yes, without its value |
| `--exclude-path \| -e` | No | \*\*/users/\*\* | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. | Yes, without its value |
| `--language \| -l` | No | csharp | The target language for the generated code files or for the information. | Yes |
| `--namespace-name \| -n` | No | Contoso.GraphApp | The namespace of the client class. Defaults to `Microsoft.Graph`. | Yes, without its value |
| `--backing-store \| -b` | No | | Defaults to `false` | Yes, without its value |
| `--exclude-backward-compatible \| --ebc` | No |  | Whether to exclude the code generated only for backward compatibility reasons or not. Defaults to `false`. | Yes |
| `--structured-media-types \| -m` | No | `application/json` |Any valid media type which will match a request body type or a response type in the OpenAPI description. Default are documented [here](https://learn.microsoft.com/en-us/openapi/kiota/using#--structured-mime-types--m). | Yes |
| `--skip-generation \| --sg` | No | true | When specified, the generation would be skipped. Defaults to false. | Yes |
| `--output \| -o` | No | ./generated/graph/csharp | The output directory or file path for the generated code files. Defaults to `./output`. | Yes |

> [!NOTE] 
> It is not required to use the CLI to edit clients. It is possible to edit a client by modifying its entry in the `clients` section of the `kiota-config.json` file. See the [kiota-config.json schema](../schemas/kiota-config.json.md) for more information.

## Using `kiota client edit`

```bash
kiota client edit --client-name "GraphClient" --exclude-path "/users/$count"
```

_The resulting `kiota-config.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
  "clients": {
    "GraphClient": {
      "descriptionLocation": "https://aka.ms/graph/v1.0/openapi.yaml",
      "includePatterns": ["**/users/**"],
      "excludePatterns": ["/users/$count"],
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
  "publisher": {
    "name": "Microsoft Graph",
    "contactEmail": "graphsdkpub@microsoft.com"
  },
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
    └─definitions
       └─GraphClient.yaml
 └─generated
    └─graph
       └─csharp
          └─... # Generated code files
          └─GraphClient.cs  
 └─apimanifest.json
 └─kiota-config.json 
```