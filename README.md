# Project

[![Dotnet](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml) [![CodeQL](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=coverage)](https://sonarcloud.io/dashboard?id=microsoft_kiota) [![Sonarcloud Status](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=alert_status)](https://sonarcloud.io/dashboard?id=microsoft_kiota)

Kiota is a project to build an OpenAPI based code generator for creating SDKs for HTTP APIs. The goal is to produce a lightweight, low maintenance, code generator that is fast enough to run as part of the compile time tool-chain but scalable enough to handle the largest APIs. Kiota generates a lightweight set of strongly typed classes that layer over a set of core HTTP libraries and produce an intuitive and discoverable way of creating HTTP requests. A set of abstractions decouple the generated service library from the core libraries allowing a variety of core libraries to be supported.

This library builds on top of the [Microsoft.OpenAPI.NET](https://github.com/microsoft/openapi.net) library to ensure comprehensive support for APIs that use OpenAPI descriptions. One of the goals of the project is to provide the best code generator support possible for OpenAPI and JSON Schema features. The [conceptual documentation](https://microsoft.github.io/kiota) describes how kiota works and the high level concepts, this readme documents how to get started with Kiota.

## Getting started

### Generating SDKs

1. Install required tools and dependencies. (refer to the [Supported Languages](#supported-languages) table under the **Required tools & dependencies** column)
1. Get Kiota using one of the [available options](https://microsoft.github.io/kiota/get-started/).
1. Generate your API client, checkout the [Parameters reference](https://microsoft.github.io/kiota/using) for the different options.
1. Start calling your API using your fluent API SDK.

### Supported languages

The following table provides an overview of the languages supported by Kiota and the progress in the implementation of the different components.

| Language | Generation | Abstractions                   | Serialization                                                   | Authentication | HTTP | Required tools & dependencies |
| -------- | ---------- |--------------------------------|-----------------------------------------------------------------| -------------- | ---- | -------------- |
| CSharp | [✔](https://github.com/microsoft/kiota/projects/5) | [✔](https://github.com/microsoft/kiota-abstractions-dotnet)     | [JSON](https://github.com/microsoft/kiota-serialization-json-dotnet), [TEXT](https://github.com/microsoft/kiota-serialization-text-dotnet)                             | [Anonymous](https://github.com/microsoft/kiota-abstractions-dotnet/blob/main/src/authentication/AnonymousAuthenticationProvider.cs), [Azure](https://github.com/microsoft/kiota-authentication-azure-dotnet) | [✔](https://github.com/microsoft/kiota-http-dotnet) | [link](https://microsoft.github.io/kiota/get-started/dotnet) |
| Go | [✔](https://github.com/microsoft/kiota/projects/8) | [✔](https://github.com/microsoft/kiota-abstractions-go)         | [JSON](https://github.com/microsoft/kiota-serialization-json-go), [TEXT](https://github.com/microsoft/kiota-serialization-text-go)                                 | [Anonymous](https://github.com/microsoft/kiota-abstractions-go/blob/main/authentication/anonymous_authentication_provider.go), [Azure](https://github.com/microsoft/kiota-authentication-azure-go/) | [✔](https://github.com/microsoft/kiota-http-go/) | [link](https://microsoft.github.io/kiota/get-started/go) |
| Java | [✔](https://github.com/microsoft/kiota/projects/7) | [✔](https://github.com/microsoft/kiota-java/tree/main/components/abstractions)       | [JSON](https://github.com/microsoft/kiota-java/tree/main/components/serialization/json), [TEXT](https://github.com/microsoft/kiota-java/tree/main/components/serialization/text)                               | [Anonymous](https://github.com/microsoft/kiota-java/blob/main/components/abstractions/src/main/java/com/microsoft/kiota/authentication/AnonymousAuthenticationProvider.java), [Azure](https://github.com/microsoft/kiota-java/tree/main/components/authentication/azure) | [✔](https://github.com/microsoft/kiota-java/tree/main/components/http/okHttp) | [link](https://microsoft.github.io/kiota/get-started/java) |
| PHP | [✔](https://github.com/microsoft/kiota/projects/4) | [✔](https://github.com/microsoft/kiota-abstractions-php)          | [JSON](https://github.com/microsoft/kiota-serialization-json-php), [TEXT](https://github.com/microsoft/kiota-serialization-text-php)                                | [Anonymous](https://github.com/microsoft/kiota-abstractions-php/blob/main/src/Authentication/AnonymousAuthenticationProvider.php), [✔️ PHP League](https://github.com/microsoft/kiota-authentication-phpleague-php) | [✔](https://github.com/microsoft/kiota-http-guzzle-php) | [link](https://microsoft.github.io/kiota/get-started/php) |
| Python | [▶](https://github.com/microsoft/kiota/projects/3) | [✔](./abstractions/python)  | [JSON](./serialization/python/json), [❌ TEXT](https://github.com/microsoft/kiota/issues/1406) | [Anonymous](./abstractions/python/kiota/abstractions/authentication/anonymous_authentication_provider.py), [Azure](./authentication/python/azure) | [✔](./http/python/requests) |  |
| Ruby | [✔](https://github.com/microsoft/kiota/projects/6) | [✔](./abstractions/ruby)       | [JSON](./serialization/ruby/json/microsoft_kiota_serialization), [❌ TEXT](https://github.com/microsoft/kiota/issues/1049) | [Anonymous](./abstractions/ruby/microsoft_kiota_abstractions/lib/microsoft_kiota_abstractions/authentication/anonymous_authentication_provider.rb), [❌ Azure](https://github.com/microsoft/kiota/issues/421) | [✔](./http/ruby/nethttp/microsoft_kiota_nethttplibrary)| [link](https://microsoft.github.io/kiota/get-started/ruby)  |
| TypeScript/JavaScript | [✔](https://github.com/microsoft/kiota/projects/2) | [✔](https://github.com/microsoft/kiota-typescript/tree/main/packages/abstractions) | [JSON](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/json), [TEXT](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/text)                         | [Anonymous](https://github.com/microsoft/kiota-typescript/blob/main/packages/abstractions/src/authentication/anonymousAuthenticationProvider.ts), [Azure](https://github.com/microsoft/kiota-typescript/tree/main/packages/authentication/azure) | [✔](https://github.com/microsoft/kiota-typescript/tree/main/packages/http/fetch) | [link](https://microsoft.github.io/kiota/get-started/typescript) |
| Shell | [✔](https://github.com/microsoft/kiota/projects/10) | [✔](./abstractions/dotnet), [✔](./cli/commonc) | [JSON](./serialization/dotnet/json), [TEXT](./serialization/dotnet/text) | [Anonymous](./abstractions/dotnet/src/authentication/AnonymousAuthenticationProvider.cs), [Azure](./authentication/dotnet/azure) | [✔](./http/dotnet/httpclient) | [link](https://microsoft.github.io/kiota/get-started/dotnet) |
| Swift | [▶](https://github.com/microsoft/kiota/issues/1449) | [✔](./abstractions/swift)       | [❌ JSON](https://github.com/microsoft/kiota/issues/1451), [❌ TEXT](https://github.com/microsoft/kiota/issues/1452) | [Anonymous](./abstractions/swift/Source/MicrosoftKiotaAbstractions/Authentication/AnonymousAuthenticationProvider.swift), [❌ Azure](https://github.com/microsoft/kiota/issues/1453) | [❌](https://github.com/microsoft/kiota/issues/1454)|  |

> Legend: ✔ -> in preview, ❌ -> not started, ▶ -> in progress.

### Parameters reference

Parameters are documented [here](https://microsoft.github.io/kiota/using).

### Debugging

Make sure you [install the pre-requisites first](https://microsoft.github.io/kiota/contributing). If you are using Visual Studio Code as your IDE, the **launch.json** file already contains the configuration to run Kiota. By default this configuration will use the `openApiDocs/v1.0/Mail.yml` under the [PowerShell repository](https://github.com/microsoftgraph/msgraph-sdk-powershell) as the OpenAPI to generate an SDK for. By default this configuration will output the generated files in a graphdotnetv4|graphjavav4|graphtypescriptv4 folder located in the parent folder this repository is cloned in.

Selecting the language you want to generate an SDK for in the Visual Studio Debug tab and hitting **F5** will automatically build, start, and attach the debugging process to Kiota.

### Samples

You can find samples of clients generated with Kiota in the [Kiota samples](https://github.com/microsoft/kiota-samples) repository.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit [https://cla.opensource.microsoft.com](https://cla.opensource.microsoft.com).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
