---
parent: Generators
---

# Running Kiota from the dotnet tool

Before you can install Kiota as a dotnet tool you will first have to create a Nuget configuration file to access the authenticated GitHub package feed. The following steps show you how to create this file. This is a temporary step until we publish the Kiota tool to the public nuget feed.

1. Navigate to [New personal access token](https://github.com/settings/tokens/new) and generate a new token. (permissions: package:read, repo).
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

1. Execute the following command to install the tool.

    ```shell
    dotnet tool install --global --configfile nuget.config kiota
    ```

1. Execute the following command to run Kiota.

    ```shell
    kiota -d /some/input/description.yml -o /some/output/path --language csharp -n samespaceprefix
    ```
