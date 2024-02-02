# info

## Description 

Show languages and runtime dependencies information.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `--openapi \| -d` | Yes | https://aka.ms/graph/v1.0/openapi.yaml | The location of the OpenAPI description in JSON or YAML format to use to generate the SDK. Accepts a URL or a local path. | Yes, without its value |
| `--language \| -l` | No | csharp | The target language for the generated code files or for the information. | Yes |
| `--clear-cache \| --cc` | No | true | Clears any cached data for the current command. Defaults to `False`. | Yes |
| `--log-level \| --ll` | No | Critical | The log level to use when logging messages to the main output. Options available: Critical, Debug, Error, Information, None, Trace & Warning. Defaults to `Warning`. | Yes |
| `--version \| --v` | No | beta | The version of the OpenAPI document to use | Yes, without its value |
| `--search-key \| --k` | No | github::microsoftgraph/msgraph-metadata/graph.microsoft.com/v1.0 | The search key used to locate the OpenAPI description. | Yes, without its value |

## Usage

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
```

The following command with the provided options will display the following result.

```bash
kiota info -l CSharp
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

Using the `--json` optional parameter render the output in a machine parsable format:

```bash
kiota info -l CSharp --json
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