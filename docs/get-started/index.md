---
title: Get started
nav_order: 2
has_children: true
---

# Get started with Kiota

Kiota can be accessed in the following ways.

- [Download binaries](#download-binaries)
- [Install as .NET tool](#install-as-net-tool)
- [Run in Docker](#run-in-docker)
- [Build Kiota](#build-kiota)

## Download binaries

You can download the latest version from the [releases page](https://github.com/microsoft/kiota/releases/latest).

## Install as .NET tool

If you have the [.NET SDK](https://dotnet.microsoft.com/download) installed, you can install Kiota as a [.NET tool](https://docs.microsoft.com/dotnet/core/tools/global-tools).

### Configure NuGet to access GitHub

Before you can install Kiota as a .NET tool you need to create a NuGet configuration file to access the authenticated GitHub package feed. The following steps show you how to create this file. **This is a temporary step until we publish the Kiota tool to the public NuGet feed.**

1. Navigate to [New personal access token](https://github.com/settings/tokens/new) and generate a new token. (permissions: `package:read`, `repo`).
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
                <!-- your github username -->
                <add key="Username" value="" />
                <!-- your github PAT: read:packages with SSO enabled for the Microsoft org
                (for microsoft employees only) -->
                <add key="ClearTextPassword" value="" />
            </GitHub>
        </packageSourceCredentials>
    </configuration>
    ```

### Install the .NET tool

Execute the following command to install the tool.

```shell
dotnet tool install --global --configfile nuget.config kiota
```

## Run in Docker

You can run Kiota in our Docker container with one of the following commands.

```shell
docker run -v /some/output/path:/app/output \
-v /some/input/description.yml:/app/openapi.yml \
ghcr.io/microsoft/kiota/generator --language csharp -n samespaceprefix
```

> **Note:** you can alternatively use the `--openapi` parameter with a URI instead of volume mapping.

To generate a SDK from an online OpenAPI description and into the current directory:

```shell
docker run -v ${PWD}:/app/output ghcr.io/microsoft/kiota/generator \
--language typescript -n gfx -d \
https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/openApiDocs/v1.0/Mail.yml
```

## Build Kiota

1. Clone the current repository.
1. Install the [.NET SDK 6.0](https://dotnet.microsoft.com/download).
1. Open the solution with Visual Studio and right click *publish* **--or--** execute the following command:

    ```shell
    dotnet publish ./src/kiota/kiota.csproj -c Release -p:PublishSingleFile=true -r win-x64
    ```

1. Navigate to the output directory (usually under `src/kiota/bin/Release/net6.0`).
1. Run `kiota.exe ...`.

> **Note:** refer to [.NET runtime identifier catalog](https://docs.microsoft.com/dotnet/core/rid-catalog) so select the appropriate runtime for your platform.

## Next steps

For details on running Kiota, see [Using the Kiota tool](..\using.md)

The following topics provide details on using Kiota to generate SDKs for specific languages.
