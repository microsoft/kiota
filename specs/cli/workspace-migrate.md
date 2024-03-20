#  `kiota workspace migrate`

This command is valuable in cases where a code base was created with Kiota v1.0 and needs to be migrated to the latest version of Kiota. The `kiota config migrate` command will identify and locate the closest `workspace.json` file available. If a file can't be found, it would initialize a new `workspace.json` file. Then, it would identify all `kiota-lock.json` files that are within this folder structure and add each of them to the `workspace.json` file. Adding the clients to the `workspace.json` file would not trigger the generation as it only affects the `workspace.json` file. The `kiota client generate` command would need to be executed to generate the code for the clients.

The API manifest won't contain any request after the migration since it could lead to misalignments between the generated client and the reported requests if the description has changed between the initial generation of the client and the migration. To get the requests populated, the user will need to use the generate command.

In the case where conflicting API client names would be migrated, the command will error out and invite the user to re-run the command providing more context for the `--client-name` parameter.

## Parameters

| Parameters | Required | Example | Description | Telemetry | 
| -- | -- | -- | -- | -- |
| `--lock-directory \| --ld` | No | ./output/pythonClient/ | Relative path to the directory containing the `kiota-lock.json` file. If not specified, all `kiota-lock.json` files within in the current directory tree will be used. | Yes, without its value |
| `--client-name \| --cn` | No | graphDelegated | Used with `--lock-directory`, it would allow to specify a name for the API client. Else, name is auto-generated as a concatenation of the `language` and `clientClassName`. | Yes, without its value |

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
      "apiDescriptionUrl": "https://aka.ms/graph/v1.0/openapi.yaml",
      "apiDeploymentBaseUrl": "https://graph.microsoft.com",
      "apiDescriptionVersion": "v1.0",
      "requests": []
    },
    "pythonGraphServiceClient": {
      "x-ms-kiotaHash": "9EDF8506CB74FE44...",
      "apiDescriptionUrl": "https://aka.ms/graph/v1.0/openapi.yaml",
      "apiDeploymentBaseUrl": "https://graph.microsoft.com",
      "apiDescriptionVersion": "v1.0",
      "requests": []
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
kiota config migrate --lock-directory ./generated/graph/csharp --client-name GraphClient
```

_The resulting `kiota-config.json` file will look like this:_

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
       └─GraphClient.yaml
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