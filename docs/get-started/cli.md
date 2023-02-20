---
parent: Get started
---

# Build SDKs for CLI

## Required tools
A commandline tool is required. We recommend:
- [Windows Terminal + version](https://apps.microsoft.com/store/detail/windows-terminal/9N0DX20HK701)

- A commandline that supports Bash?
- MacOS terminal
- PowerShell version 7.3.2

## Target project requirements
Download the CLI SDK to get started for 
Windows, Mac, or Linux here on the assets link of the latest release: [CLI SDK Download](https://github.com/microsoftgraph/msgraph-cli/releases)


## Adding dependencies

### Getting access to the packages

### Add Kiota and dependencies

> **Note:** Find current version numbers for Kiota packages at [Nexus Repository Manager](https://oss.sonatype.org/).


## Generating the SDK

Kiota generates SDKs from OpenAPI documents. Create a file named **getme.yml** and add the contents of the [Sample OpenAPI description](reference-openapi.md).

You can then use the Kiota command line tool to generate the SDK classes.

```bash
#TBD
```

## Registering an application in Azure AD

## Creating an application registration

> **Note:** this step is required if your client will be calling APIs that are protected by the Microsoft Identity Platform like Microsoft Graph.

Follow the instructions in [Register an application for Microsoft identity platform authentication](register-app.md) to get an application ID (also know as a client ID).

## Creating the client application

Replace `YOUR_CLIENT_ID` with the client ID from your app registration.

``` bash
mgc login --client-id `YOUR_CLIENT_ID` --tenant-id `YOUR_TENANT_ID` --scopes User.ReadWrite --scopes Mail.ReadWrite
```

## Executing the application
Navigate to your download folder and use the **mgc** command to run commands on the CLI SDK. 
Add -h to your commands to view additional commands.


### Authentication
The SDK supports both delegeted and app-only authentication strategies. Run the command below to see supported authentication strategies.

``` bash
#View supported authentication strategies. Default is Device Code
mgc login --strategy 
```


#### **Delegated access**
**1. DeviceCode authentication strategy**


``` bash
#Using the Default authentication (which is Device Code)
mgc login
```
OR

``` bash
#Using the DeviceCode authentication  explicitly
mgc login --strategy DeviceCode
```

**2.Interactive Browser authentication strategy**
``` bash
# Using interactive Browser authentication.
mgc login --strategy InteractiveBrowser

```

#### **App-only access**
**1. Client Certificate authentication strategy**
``` bash
# Using Client Certificate authentication.
mgc login --strategy ClientCertificate

```
### Samples
You can find additional samples here: [CLI SDK samples](https://github.com/microsoftgraph/msgraph-cli/tree/main/samples)


## See also

- [kiota-samples repository](https://github.com/microsoft/kiota-samples/tree/main/get-started) contains the code from this guide.
- [ToDoItem Sample API](https://github.com/microsoft/kiota-samples/tree/main/sample-api) implements a sample OpenAPI in ASP.NET Core and sample clients in multiple languages.
