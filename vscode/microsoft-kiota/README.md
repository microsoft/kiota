# Microsoft Kiota

OpenAPI based client generator for REST APIs.

## Features

Using kiota you can:

1. Search for API descriptions.
1. Select the API endpoints you need.
1. Generate models and a chained method API surface in the language of your choice.
1. Call the API with the new client.

All that in a matter of seconds.

## Requirements

None.

## Extension Settings

None.

## Known Issues

Only tested for Visual Studio Code locally. (no remote experience, no containers, no web experience)

Checkout the [list of open issues](https://github.com/microsoft/kiota/issues) to get a list of the known issues.

## Release Notes

Checkout the [release notes](https://github.com/microsoft/kiota/releases) to get more information about each release.

## Debugging the extension

1. in `src/kiota` run `dotnet watch publish -p:PublishSingleFile=true -p:PublishReadyToRun=true -c Release -r <win-x64|linux-x64|osx-x64> -o <pathToThisFolder>\.kiotabin\<versionInPackage.json>\<rid>\`
1. in `vscode/microsoft-kiota` run `code . && npm i`
1. in the code instance that was opened at step 2, just press f5
