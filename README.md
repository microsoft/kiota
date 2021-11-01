# Project

[![Dotnet](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml) [![CodeQL](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=coverage)](https://sonarcloud.io/dashboard?id=microsoft_kiota) [![Sonarcloud Status](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=alert_status)](https://sonarcloud.io/dashboard?id=microsoft_kiota)

Kiota is a project to build an OpenAPI based code generator for creating SDKs for HTTP APIs. The goal is to produce a lightweight, low maintenance, code generator that is fast enough to run as part of the compile time tool-chain but scalable enough to handle the largest APIs. Kiota generates a lightweight set of strongly typed classes that layer over a set of core HTTP libraries and produce an intuitive and discoverable way of creating HTTP requests. A set of abstractions decouple the generated service library from the core libraries allowing a variety of core libraries to be supported.

This library builds on top of the [Microsoft.OpenAPI.NET](https://github.com/microsoft/openapi.net) library to ensure comprehensive support for APIs that use OpenAPI descriptions. One of the goals of the project is to provide the best code generator support possible for OpenAPI and JSON Schema features. The [conceptual documentation](https://microsoft.github.io/kiota) describes how kiota works and the high level concepts, this readme documents how to get started with Kiota.

## Getting started

### Generating SDKs

1. Install required tools and dependencies. (refer to the [Supported Languages](#supported-languages) table under the **Required tools & dependencies** column)
1. Get Kiota: [download and run binaries](./docs/generator/binaries.md) **--or--** [run the docker image](./docs/generator/docker.md)  **--or--** [install and run the dotnet tool](./docs/generator/tool.md) **--or--** clone the repository and [build Kiota locally](./docs/generator/build.md).
1. Generate your API client, checkout the [Parameters reference](#parameters-reference) for the different options.
1. Start calling your API using your fluent API SDK.

### Supported languages

The following table provides an overview of the languages supported by Kiota and the progress in the implementation of the different components.

| Language | Generation | Abstractions | Serialization | Authentication | HTTP | Required tools & dependencies |
| -------- | ---------- | ------------ | ------------- | -------------- | ---- | -------------- |
| CSharp | [✔](https://github.com/microsoft/kiota/projects/5) | [✔](./abstractions/dotnet) | [JSON](./serialization/dotnet/json) | [Anonymous](./abstractions/dotnet/src/authentication/AnonymousAuthenticationProvider.cs), [Azure](./authentication/dotnet/azure) | [✔](./http/dotnet/httpclient) | [link](./docs/requiredtools/dotnet.md) |
| Go | [✔](https://github.com/microsoft/kiota/projects/8) | [✔](./abstractions/go) | [JSON](./serialization/go/json) | [Anonymous](./abstractions/go/authentication/anonymous_authentication_provider.go), [Azure](./authentication/go/azure) | [✔](./http/go/nethttp) | [link](./docs/requiredtools/go.md) |
| Java | [✔](https://github.com/microsoft/kiota/projects/7) | [✔](./abstractions/java) | [JSON](./serialization/java/json) | [Anonymous](./abstractions/java/lib/src/main/java/com/microsoft/kiota/authentication/AnonymousAuthenticationProvider.java), [Azure](./authentication/java/azure) | [✔](./http/java/okhttp) | [link](./docs/requiredtools/java.md) |
| PHP | [❌](https://github.com/microsoft/kiota/projects/4) | [▶](https://github.com/microsoft/kiota/pull/321) | ❌ | ❌ | ❌ |  |
| Python | [❌](https://github.com/microsoft/kiota/projects/3) | ❌ | ❌ | ❌ | ❌ |  |
| Ruby | [✔](https://github.com/microsoft/kiota/projects/6) | [✔](./abstractions/ruby) | [JSON](./serialization/ruby/json/microsoft_kiota_serialization) | [Anonymous](./abstractions/ruby/microsoft_kiota_abstractions/lib/microsoft_kiota_abstractions/authentication/anonymous_authentication_provider.rb), [❌ Azure](https://github.com/microsoft/kiota/issues/421) | [✔](./http/ruby/nethttp/microsoft_kiota_nethttplibrary)| [link](./docs/requiredtools/ruby.md)  |
| TypeScript/JavaScript | [✔](https://github.com/microsoft/kiota/projects/2) | [✔](./abstractions/typescript) | [JSON](./serialization/typescript/json) | [Anonymous](./abstractions/typescript/src/authentication/anonymousAuthenticationProvider.ts), [Azure](./authentication/typescript/azure) | [✔](./http/typescript/fetch) | [link](./docs/requiredtools/typescript.md) |

> Legend: ✔ -> in preview, ❌ -> not started, ▶ -> in progress.

### Parameters reference

Kiota accepts the following parameters during the generation:

| Name | Shorthand | Required | Description | Accepted values | Default Value |
| ---- | --------- | -------- | ----------- | --------------- | ------------- |
| backing-store | b | no | Enables backing store for models. | Flag. N/A. | false |
| class-name | c | no | The class name to use the for main entry point | A valid class name according to the target language specification. | ApiClient |
| deserializer | ds | no | The fully qualified class names for deserializers. | This parameter can be passed multiple values. A module name like `Microsoft.Kiota.Serialization.Json` that implementats `IParseNodeFactory`. | `Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory` (csharp), `@microsoft/kiota-serialization-json.JsonParseNodeFactory` (typescript), `com.microsoft.kiota.serialization.JsonParseNodeFactory` (java), `github.com/microsoft/kiota/serialization/go/json.JsonParseNodeFactory` (go) |
| language | l | no | The programming language to generate the SDK in. | csharp, java, or typescript | csharp |
| loglevel | ll | no | The log level to use when logging events to the main output. | Microsoft.Extensions.Logging.LogLevel values | Warning |
| namespace-name | n | no | The namespace name to use the for main entry point. | Valid namespace/module name according to target language specifications. | ApiSdk |
| openapi | d | no | URI or Path to the OpenAPI description (JSON or YAML) to use to generate the SDK. | A valid URI pointing to an HTTP document or a file on the local file-system. | ./openapi.yml |
| output | o | no | The output path of the folder the code will be generated in. The folders will be created during the generation if they don't already exist. | A valid path to a folder. | ./output |
| serializer | s | no | The fully qualified class names for serializers. | This parameter can be passed multiple values. A class name like `Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory` that implementats `ISerializationWriterFactory`. | `Microsoft.Kiota.Serialization.Json` (csharp), `@microsoft/kiota-serialization-json.JsonSerializationWriterFactory` (typescript), `com.microsoft.kiota.serialization.JsonSerializationWriterFactory` (java), `github.com/microsoft/kiota/serialization/go/json.JsonSerializationWriterFactory` (go) |

### Debugging

Make sure you [install the pre-requisites first](./docs/requiredtools/kiota.md). If you are using Visual Studio Code as your IDE, the **launch.json** file already contains the configuration to run Kiota. By default this configuration will use the `openApiDocs/v1.0/Mail.yml` under the [PowerShell repository](https://github.com/microsoftgraph/msgraph-sdk-powershell) as the OpenAPI to generate an SDK for. By default this configuration will output the generated files in a graphdotnetv4|graphjavav4|graphtypescriptv4 folder located in the parent folder this repository is cloned in.

Selecting the language you want to generate an SDK for in the Visual Studio Debug tab and hitting **F5** will automatically build, start, and attach the debugging process to Kiota.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
