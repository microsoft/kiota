# Project

[![Dotnet](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml) [![CodeQL](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=coverage)](https://sonarcloud.io/dashboard?id=microsoft_kiota) [![Sonarcloud Status](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=alert_status)](https://sonarcloud.io/dashboard?id=microsoft_kiota)

Kiota is a command line tool for generating an API client to call any OpenAPI described API you are interested in. The goal is to eliminate the need to take a dependency on a different API SDK for every API that you need to call. Kiota API clients provide a strongly typed experience with all the features you expect from a high quality API SDK, but without having to learn a new library for every HTTP API.

This library builds on top of the [Microsoft.OpenAPI.NET](https://github.com/microsoft/openapi.net) library to ensure comprehensive support for APIs that use OpenAPI descriptions. One of the goals of the project is to provide the best code generator support possible for OpenAPI and JSON Schema features. The [conceptual documentation](https://learn.microsoft.com/openapi/kiota) describes how kiota works and the high level concepts, this readme documents how to get started with Kiota.

## Getting started

### Generating SDKs

1. Install required tools and dependencies. (refer to the [Supported Languages](#supported-languages) table under the **Required tools & dependencies** column)
1. Get Kiota using one of the [available options](https://learn.microsoft.com/openapi/kiota/install).
1. Generate your API client, checkout the [Parameters reference](https://learn.microsoft.com/openapi/kiota/using) for the different options.
1. Start calling your API using your fluent API Client.

### Supported languages

The following table provides an overview of the languages supported by Kiota and the progress in the implementation of the different components.

| Language | Generation | Abstractions                   | Serialization                                                                                                                                                                                                                                                                                                                                                                                | Authentication | HTTP | Required tools & dependencies |
| -------- | ---------- |--------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------| -------------- | ---- | -------------- |
| CSharp | ✔ | [✔](https://github.com/microsoft/kiota-dotnet/tree/main/src/abstractions)     | [FORM](https://github.com/microsoft/kiota-dotnet/tree/main/src/serialization/form), [JSON](https://github.com/microsoft/kiota-dotnet/tree/main/src/serialization/json), [MULTIPART](https://github.com/microsoft/kiota-dotnet/tree/main/src/serialization/multipart), [TEXT](https://github.com/microsoft/kiota-dotnet/tree/main/src/serialization/text)                                     | [Anonymous](https://github.com/microsoft/kiota-dotnet/blob/main/src/abstractions/authentication/AnonymousAuthenticationProvider.cs), [API Key](https://github.com/microsoft/kiota-dotnet/blob/main/src/abstractions/authentication/ApiKeyAuthenticationProvider.cs), [Azure](https://github.com/microsoft/kiota-dotnet/tree/main/src/authentication/azure) | [✔](https://github.com/microsoft/kiota-dotnet/tree/main/src/http/httpClient) | [link](https://learn.microsoft.com/openapi/kiota/quickstarts/dotnet) |
| Go | ✔ | [✔](https://github.com/microsoft/kiota-abstractions-go)         | [FORM](https://github.com/microsoft/kiota-serialization-form-go), [JSON](https://github.com/microsoft/kiota-serialization-json-go), [MULTIPART](https://github.com/microsoft/kiota-serialization-multipart-go), [TEXT](https://github.com/microsoft/kiota-serialization-text-go)                                                                                                             | [Anonymous](https://github.com/microsoft/kiota-abstractions-go/blob/main/authentication/anonymous_authentication_provider.go), [API Key](https://github.com/microsoft/kiota-abstractions-go/blob/main/authentication/api_key_authentication_provider.go), [Azure](https://github.com/microsoft/kiota-authentication-azure-go/) | [✔](https://github.com/microsoft/kiota-http-go/) | [link](https://learn.microsoft.com/openapi/kiota/quickstarts/go) |
| Java | ✔ | [✔](https://github.com/microsoft/kiota-java/tree/main/components/abstractions)       | [FORM](https://github.com/microsoft/kiota-java/tree/main/components/serialization/form), [JSON](https://github.com/microsoft/kiota-java/tree/main/components/serialization/json), [MULTIPART](https://github.com/microsoft/kiota-java/tree/main/components/serialization/multipart), [TEXT](https://github.com/microsoft/kiota-java/tree/main/components/serialization/text)                 | [Anonymous](https://github.com/microsoft/kiota-java/blob/main/components/abstractions/src/main/java/com/microsoft/kiota/authentication/AnonymousAuthenticationProvider.java), [API Key](https://github.com/microsoft/kiota-java/blob/main/components/abstractions/src/main/java/com/microsoft/kiota/authentication/ApiKeyAuthenticationProvider.java), [Azure](https://github.com/microsoft/kiota-java/tree/main/components/authentication/azure) | [✔](https://github.com/microsoft/kiota-java/tree/main/components/http/okHttp) | [link](https://learn.microsoft.com/openapi/kiota/quickstarts/java) |
| PHP | ✔ | [✔](https://github.com/microsoft/kiota-abstractions-php)          | [JSON](https://github.com/microsoft/kiota-serialization-json-php), [FORM](https://github.com/microsoft/kiota-serialization-form-php), [MULTIPART](https://github.com/microsoft/kiota-serialization-multipart-php), [TEXT](https://github.com/microsoft/kiota-serialization-text-php)                                                                                                         | [Anonymous](https://github.com/microsoft/kiota-abstractions-php/blob/main/src/Authentication/AnonymousAuthenticationProvider.php), [✔️ PHP League](https://github.com/microsoft/kiota-authentication-phpleague-php) | [✔](https://github.com/microsoft/kiota-http-guzzle-php) | [link](https://learn.microsoft.com/openapi/kiota/quickstarts/php) |
| Python | ✔ | [✔](https://github.com/microsoft/kiota-abstractions-python)  | [FORM](https://github.com/microsoft/kiota-serialization-form-python), [JSON](https://github.com/microsoft/kiota-serialization-json-python), [MULTIPART](https://github.com/microsoft/kiota-serialization-multipart-python), [TEXT](https://github.com/microsoft/kiota-serialization-text-python)                                                                                             | [Anonymous](https://github.com/microsoft/kiota-abstractions-python/blob/main/kiota_abstractions/authentication/anonymous_authentication_provider.py), [Azure](https://github.com/microsoft/kiota-authentication-azure-python) | [✔](https://github.com/microsoft/kiota-http-python) | [link](https://learn.microsoft.com/openapi/kiota/quickstarts/python) |
| Ruby | 🛠️ | [🛠️](https://github.com/microsoft/kiota-abstractions-ruby)       | [❌ FORM](https://github.com/microsoft/kiota/issues/2077), [JSON](https://github.com/microsoft/kiota-serialization-json-ruby), [❌ MULTIPART](https://github.com/microsoft/kiota/issues/3032), [❌ TEXT](https://github.com/microsoft/kiota/issues/1049)                                                                                                                                        | [Anonymous](https://github.com/microsoft/kiota-abstractions-ruby/blob/main/lib/microsoft_kiota_abstractions/authentication/anonymous_authentication_provider.rb), [OAuth2](https://github.com/microsoft/kiota-authentication-oauth-ruby) | [🛠️](https://github.com/microsoft/kiota-http-ruby)|  |
| CLI | 🛠️ | (see CSharp) + [🛠️](https://github.com/microsoft/kiota-cli-commons) | (see CSharp)                                                                                                                                                                                                                                                                                                                                                                                 | (see CSharp) | (see CSharp) | [link](https://learn.microsoft.com/openapi/kiota/quickstarts/cli) |
| Swift | [❌](https://github.com/microsoft/kiota/issues/1449) | [🛠️](./abstractions/swift)       | [❌ FORM](https://github.com/microsoft/kiota/issues/2076), [❌ JSON](https://github.com/microsoft/kiota/issues/1451), [❌ FORM](https://github.com/microsoft/kiota/issues/3033), [❌ TEXT](https://github.com/microsoft/kiota/issues/1452)                                                                                                                                                       | [Anonymous](./abstractions/swift/Source/MicrosoftKiotaAbstractions/Authentication/AnonymousAuthenticationProvider.swift), [❌ Azure](https://github.com/microsoft/kiota/issues/1453) | [❌](https://github.com/microsoft/kiota/issues/1454)|  |
| TypeScript/JavaScript | 🛠️ | [🛠️](https://github.com/microsoft/kiota-typescript/tree/main/packages/abstractions) | [FORM](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/form), [JSON](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/json), [MULTIPART](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/multipart), [TEXT](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/text) | [Anonymous](https://github.com/microsoft/kiota-typescript/blob/main/packages/abstractions/src/authentication/anonymousAuthenticationProvider.ts), [API Key](https://github.com/microsoft/kiota-typescript/blob/main/packages/abstractions/src/authentication/apiKeyAuthenticationProvider.ts), [Azure](https://github.com/microsoft/kiota-typescript/tree/main/packages/authentication/azure), [SPFx](https://github.com/microsoft/kiota-typescript/tree/main/packages/authentication/spfx) | [🛠️](https://github.com/microsoft/kiota-typescript/tree/main/packages/http/fetch) | [link](https://learn.microsoft.com/openapi/kiota/quickstarts/typescript) |

> Legend: ✔ -> stable, 🛠️ -> in preview, ❌ -> not started, ▶ -> in progress.

### Parameters reference

Parameters are documented [here](https://learn.microsoft.com/openapi/kiota/using).

### Debugging

Make sure you [install the pre-requisites first](CONTRIBUTING.md). If you are using Visual Studio Code as your IDE, the **launch.json** file already contains the configuration to run Kiota. By default this configuration will use the `openApiDocs/v1.0/Mail.yml` under the [PowerShell repository](https://github.com/microsoftgraph/msgraph-sdk-powershell) as the OpenAPI to generate an SDK for. By default this configuration will output the generated files in a graphdotnetv4|graphjavav4|graphtypescriptv4 folder located in the parent folder this repository is cloned in.

Selecting the language you want to generate an API client for in the Visual Studio Debug tab and hitting **F5** will automatically build, start, and attach the debugging process to Kiota.

### Samples

You can find samples of clients generated with Kiota in the [Kiota samples](https://github.com/microsoft/kiota-samples) repository.

An example of an application that is calling multiple API can be found in the [KiotaApp](https://github.com/darrelmiller/KiotaApp) repo

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
