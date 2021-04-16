# Project

[![Dotnet](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml) [![CodeQL](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=coverage)](https://sonarcloud.io/dashboard?id=microsoft_kiota) [![Sonarcloud Status](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=alert_status)](https://sonarcloud.io/dashboard?id=microsoft_kiota)

Kiota is project to build an OpenAPI based code generator for creating SDKs for HTTP APIs. The goal is to produce a lightweight, low maintenance, code generator that is fast enough to run as part of the compile time tool-chain but scalable enough to handle the largest APIs. Kiota generates a lightweight set of strongly typed classes that layer over a core HTTP library and product an intuitive and discoverable way of creating HTTP requests. A set of abstractions decouple the generated service library from the core allowing a variety of core libraries to be supported.

This library builds on top of the [Microsoft.OpenAPI.NET](https://github.com/microsoft/openapi.net) library to ensure comprehensive support for APIs that use OpenAPI descriptions. One of the goals of the project is to provide the best code generator support possible for OpenAPI and JSON Schema features.

## Getting started

### Required tools

- [.NET SDK 5.0](https://dotnet.microsoft.com/download) *
- [Visual Studio Code](https://code.visualstudio.com/)
- [Microsoft Graph PowerShell SDK](https://github.com/microsoftgraph/msgraph-sdk-powershell), cloned into the same parent folder of this repository.

#### TypeScript tools

- [NodeJS 14](https://nodejs.org/en/) *
- [TypeScript](https://www.typescriptlang.org/) `npm i -g typescript` *

#### Java tools

- [JDK 16](https://adoptopenjdk.net/) *
- [Gradle 7](https://gradle.org/install/) *

#### Dotnet tools

No additional tools are required for dotnet projects.

> Note: tools marked with * are required.

### Debugging

If you are using Visual Studio Code as your IDE, the **launch.json** file already contains the configuration to run Kiota. By default this configuration will use the `openApiDocs/v1.0/Mail.yml` under the PowerShell repository as the OpenAPI to generate an SDK for. By default this configuration will output the generated files in a graphdotnetv4|graphjavav4|graphtypescriptv4 folder located in the parent folder this repository is cloned in.

Selecting the language you want to generate an SDK for in the Visual Studio Debug tab and hitting **F5** will automatically build, start, and attach the debugging process to Kiota.

### Initializing targed projects

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](./abstractions) and the [core](./core) package from the GitHub feed.

#### TypeScript initialization

Clone a NodeJS/front end TypeScript starter like [this one](https://github.com/FreekMencke/node-typescript-starter).

```Shell
npm i @azure/identity node-fetch
```

#### Java initialization

Execute the following command in the directory you want to iniatilize the project in.

```Shell
gradle init
# Select a console application
```

Edit `utilities/build.gradle` to add the following dependencies.

```Groovy
api 'com.google.code.findbugs:jsr305:3.0.2'
api 'com.azure:azure-identity:1.2.5'
api 'com.squareup.okhttp3:okhttp:4.9.1'
api 'com.google.code.gson:gson:2.8.6'
```

#### Dotnet initilization

Execute the following command in the directory you want to initialize the project in.

```Shell
dotnet new console
dotnet new gitignore
```

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
