# `kiota info`

## Description

`kiota info` provides information about the available languages in Kiota and the dependencies that are used by the core libraries. This command is useful to get a sense of the maturity of languages in Kiota and to get language-specific information about their dependencies.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--language \| -l` | No | csharp | The language to get information about. |
| `--json` | No | | When specified, the output will be in JSON format. |
| `--ignore-dependencies` | No | | When specified, ignore the dependencies indicated in the `x-ms-kiota-info` extension (if any). |


## Using `kiota info`

```bash
kiota info
```

```bash
Language    Maturity Level
CLI         Preview
CSharp      Stable
Go          Stable
Java        Preview
PHP         Stable
Python      Stable
Ruby        Experimental
Swift       Experimental
TypeScript  Experimental

Hint: use the language argument to get the list of dependencies you need to add to your project.
Example: kiota info -l <language>
```

## Using `kiota info -l <language>`

```bash
kiota info -l csharp
```

```bash
The language CSharp is currently in Stable maturity level.
After generating code for this language, you need to install the following packages:
dotnet add package Microsoft.Kiota.Abstractions --version 1.6.1
dotnet add package Microsoft.Kiota.Http.HttpClientLibrary --version 1.3.0
dotnet add package Microsoft.Kiota.Serialization.Form --version 1.1.0
dotnet add package Microsoft.Kiota.Serialization.Json --version 1.1.1
dotnet add package Microsoft.Kiota.Authentication.Azure --version 1.1.0
dotnet add package Microsoft.Kiota.Serialization.Text --version 1.1.0
dotnet add package Microsoft.Kiota.Serialization.Multipart --version 1.1.0
```

## Using `kiota info -l <language> --json`

```bash
kiota info -l csharp --json
```

```json
{
  "maturityLevel": "Stable",
  "dependencyInstallCommand": "dotnet add package {0} --version {1}",
  "dependencies": [
    {
      "name": "Microsoft.Kiota.Abstractions",
      "version": "1.6.1"
    },
    {
      "name": "Microsoft.Kiota.Http.HttpClientLibrary",
      "version": "1.2.0"
    },
    {
      "name": "Microsoft.Kiota.Serialization.Form",
      "version": "1.1.0"
    },
    {
      "name": "Microsoft.Kiota.Serialization.Json",
      "version": "1.1.1"
    },
    {
      "name": "Microsoft.Kiota.Authentication.Azure",
      "version": "1.1.0"
    },
    {
      "name": "Microsoft.Kiota.Serialization.Text",
      "version": "1.1.0"
    },
    {
      "name": "Microsoft.Kiota.Serialization.Multipart",
      "version": "1.1.0"
    }
  ],
  "clientClassName": "",
  "clientNamespaceName": ""
}
```

## Using `kiota info -l <language> --openapi <openapi>`

```bash
kiota info -l csharp --openapi https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml
```

_Assuming these extensions would be available in the OpenAPI description:_

```yaml
openapi: 3.0.3
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
x-ms-kiota-info:
  languagesInformation:
    CSharp:
      clientClassName: graphClient
      clientNamespaceName: Microsoft.Graph
      dependencyInstallCommand: dotnet add package {name} --version {version}
      dependencies:
        - name: Microsoft.Graph.Core
          version: 3.0.0
      structuredMimeTypes:
        - application/json
servers:
  - url: https://graph.microsoft.com/v1.0
```

```bash
The language CSharp is currently in Stable maturity level.
After generating code for this language, you need to install the following packages:
dotnet add package Microsoft.Graph.Core --version 3.0.0
```

## Using `kiota info -l <language> --openapi <openapi> --ignore-dependencies`

```bash
kiota info -l csharp --openapi https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml --ignore-dependencies
```

_Assuming these extensions would be available in the OpenAPI description:_

```yaml
openapi: 3.0.3
info:
  title: OData Service for namespace microsoft.graph
  description: This OData service is located at https://graph.microsoft.com/v1.0
  version: 1.0.1
x-ms-kiota-info:
  languagesInformation:
    CSharp:
      clientClassName: graphClient
      clientNamespaceName: Microsoft.Graph
      dependencyInstallCommand: dotnet add package {name} --version {version}
      dependencies:
        - name: Microsoft.Graph.Core
          version: 3.0.0
      structuredMimeTypes:
        - application/json
servers:
  - url: https://graph.microsoft.com/v1.0
```

```bash
The language CSharp is currently in Stable maturity level.
After generating code for this language, you need to install the following packages:
dotnet add package Microsoft.Kiota.Abstractions --version 1.6.1
dotnet add package Microsoft.Kiota.Http.HttpClientLibrary --version 1.3.0
dotnet add package Microsoft.Kiota.Serialization.Form --version 1.1.0
dotnet add package Microsoft.Kiota.Serialization.Json --version 1.1.1
dotnet add package Microsoft.Kiota.Authentication.Azure --version 1.1.0
dotnet add package Microsoft.Kiota.Serialization.Text --version 1.1.0
dotnet add package Microsoft.Kiota.Serialization.Multipart --version 1.1.0
```