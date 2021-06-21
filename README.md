# Project

[![Dotnet](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml) [![CodeQL](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=coverage)](https://sonarcloud.io/dashboard?id=microsoft_kiota) [![Sonarcloud Status](https://sonarcloud.io/api/project_badges/measure?project=microsoft_kiota&metric=alert_status)](https://sonarcloud.io/dashboard?id=microsoft_kiota)

Kiota is a project to build an OpenAPI based code generator for creating SDKs for HTTP APIs. The goal is to produce a lightweight, low maintenance, code generator that is fast enough to run as part of the compile time tool-chain but scalable enough to handle the largest APIs. Kiota generates a lightweight set of strongly typed classes that layer over a set of core HTTP libraries and produce an intuitive and discoverable way of creating HTTP requests. A set of abstractions decouple the generated service library from the core libraries allowing a variety of core libraries to be supported.

This library builds on top of the [Microsoft.OpenAPI.NET](https://github.com/microsoft/openapi.net) library to ensure comprehensive support for APIs that use OpenAPI descriptions. One of the goals of the project is to provide the best code generator support possible for OpenAPI and JSON Schema features.

## Getting started

### Required tools

- [.NET SDK 5.0](https://dotnet.microsoft.com/download) *
- [Visual Studio Code](https://code.visualstudio.com/)
- [Microsoft Graph PowerShell SDK](https://github.com/microsoftgraph/msgraph-sdk-powershell), cloned into the same parent folder of this repository. This dependency is only required if you want to generate SDKs for Microsoft Graph.

#### TypeScript tools

- [NodeJS 14](https://nodejs.org/en/) *
- [TypeScript](https://www.typescriptlang.org/) `npm i -g typescript` *

#### Java tools

- [JDK 16](https://adoptopenjdk.net/) *
- [Gradle 7](https://gradle.org/install/) *

#### Dotnet tools

No additional tools are required for dotnet projects.

> Note: tools marked with * are required.

### Generating SDKs

You can either clone the repository and [build Kiota locally](#building-kiota), [download and run binaries](#running-kiota-from-binaries), [install and run the dotnet tool](#running-kiota-from-the-dotnet-tool) or [run the docker image](#running-kiota-with-docker).

#### Running Kiota from the dotnet tool

1. Navigate to [New personal access token](https://github.com/settings/tokens/new) and generate a new token. (permissions: read:package).
1. Copy the token, you will need it later.
1. Enable the SSO on the token if you are a Microsoft employee.
1. Create a `nuget.config` file in the current directory with the following content.

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
        <packageSources>
            <add key="GitHub" value="https://nuget.pkg.github.com/microsoft/index.json" />
        </packageSources>
        <packageSourceCredentials>
            <GitHub>
                <add key="Username" value="" /><!-- your github username -->
                <!-- your github PAT: read:pacakges with SSO enabled for the Microsoft org (for microsoft employees only) -->
                <add key="ClearTextPassword" value="" />
            </GitHub>
        </packageSourceCredentials>
    </configuration>
    ```

1. Execute the following command to install the tool.

    ```Shell
    dotnet tool install --global --configfile nuget.config kiota
    ```

1. Execute the following command to run kiota.

    ```Shell
    kiota -d /some/input/description.yml -o /some/output/path --language csharp -n samespaceprefix
    ```

#### Running Kiota with Docker

1. Navigate to [New personal access token](https://github.com/settings/tokens/new) and generate a new token. (permissions: read:package).
1. Copy the token, you will need it later.
1. Enable the SSO on the token if you are a Microsoft employee.
1. Execute the following command to login to the registry.

    ```Shell
    echo "<the personal access token>" | docker login "https://docker.pkg.github.com/microsoft/kiota/generator" -u "<your github username>" --password-stdin
    ```

1. Execute the following command to start generating SDKs

    ```Shell
    docker run -v /some/output/path:/app/output -v /some/input/description.yml:/app/openapi.yml docker.pkg.github.com/microsoft/kiota/generator --language csharp -n samespaceprefix
    ```

    > Note: you can alternatively use the --openapi parameter with a URI instead of volume mapping.

> Note: steps 1-4 only need to be done once per machine.

#### Building Kiota

First, clone the current repository. You can either use Visual Studio Code or Visual Studio or execute the following commands:

```Shell
dotnet publish ./src/kiota/kiota.csproj -c Release -p:PublishSingleFile=true -r win-x64
```

> Note: refer to [.NET runtime identifier catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) so select the appropriate runtime for your platform.

Navigate to the output directory (usually under `src/kiota/bin/Release/net5.0`) and start generating SDKs by running Kiota.

#### Running Kiota from binaries

If you haven't built kiota locally, select the appropriate version from the [releases page](https://github.com/microsoft/kiota/releases).

```Shell
kiota.exe -d ../msgraph-sdk-powershell/openApiDocs/v1.0/mail.yml --language csharp -o ../somepath -n namespaceprefix
```

> Note: once your SDK is generated in your target project, you will need to add references to kiota abstractions and kiota http, serialization and authentication in your project. Refer to [Initializing target projects](#initializing-target-projects)

#### Parameters reference

Kiota accepts the following parameters during the generation:

| Name | Shorthand | Required | Description | Accepted values | Default Value |
| ---- | --------- | -------- | ----------- | --------------- | ------------- |
| backing-store | b | no | The fully qualified name for the backing store class to use. | A fully qualified class name like `Microsoft.Kiota.Abstractions.Store.InMemoryBackingStore` (CSharp), `com.microsoft.kiota.store.InMemoryBackingStore` (Java), `@microsoft/kiota-abstractions.InMemoryBackingStore` (TypeScript) | Empty string |
| class-name | c | no | The class name to use the for main entry point | A valid class name according to the target language specification. | ApiClient |
| language | l | no | The programming language to generate the SDK in. | csharp, java, or typescript | csharp |
| loglevel | ll | no | The log level to use when logging events to the main output. | Microsoft.Extensions.Logging.LogLevel values | Warning |
| namespace-name | n | no | The namespace name to use the for main entry point. | Valid namespace/module name according to target language specifications. | ApiClient |
| openapi | d | no | URI or Path to the OpenAPI description (JSON or YAML) to use to generate the SDK. | A valid URI pointing to an HTTP document or a file on the local file-system. | ./openapi.yml |
| output | o | no | The output path of the folder the code will be generated in. The folders will be created during the generation if they don't already exist. | A valid path to a folder. | ./output |

### Debugging

If you are using Visual Studio Code as your IDE, the **launch.json** file already contains the configuration to run Kiota. By default this configuration will use the `openApiDocs/v1.0/Mail.yml` under the PowerShell repository as the OpenAPI to generate an SDK for. By default this configuration will output the generated files in a graphdotnetv4|graphjavav4|graphtypescriptv4 folder located in the parent folder this repository is cloned in.

Selecting the language you want to generate an SDK for in the Visual Studio Debug tab and hitting **F5** will automatically build, start, and attach the debugging process to Kiota.

### Initializing target projects

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](./abstractions) and the [authentication](./authentication), [http](./http), [serialization](./serialization) packages from the GitHub feed.

#### TypeScript initialization

Clone a NodeJS/front end TypeScript starter like [this one](https://github.com/FreekMencke/node-typescript-starter).

```Shell
npm i @azure/identity node-fetch
```

#### Java initialization

Execute the following command in the directory you want to initialize the project in.

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

#### Dotnet initialization

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
