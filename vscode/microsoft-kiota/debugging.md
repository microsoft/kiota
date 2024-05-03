# Debugging the extension

## Running in debug mode

Context: you're a developer working on the extension, and you want to debug/step through the extension code.

1. run `dotnet publish ./src/kiota/kiota.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained -c Release -r <win-x64|linux-x64|osx-x64> -o ./vscode/microsoft-kiota/.kiotabin/<versionInPackage.json>/<rid>/`
1. in `vscode/microsoft-kiota` run `code . && npm i`
1. in the code instance that was opened at step 2, just press f5

## Side loading the extension

Context: you're most likely a program manager eager to demo the latest bits from an active development branch.

### Pre-requisites

- GitHub CLI `winget install GitHub.CLI`
- dotnet 8 `winget install Microsoft.DotNet.SDK.8`
- node 20 `winget install CoreyButler.NVMforWindows && nvm install lts && nvm use lts`
- vsce & TypeScript `npm i -g TypeScript @vscode/vsce`

### Steps

#### Building and side-loading the extension

1. make sure to uninstall any version of the extension, including from remote environments if you're remoting into WSL or a devcontainer, to clear any confusion.
1. clone this repository if you don't have it locally already `gh repo clone microsoft/kiota && cd kiota`.
1. switch to the development branch `git switch branchName` (you can find the branch name in the pull request).
1. cd into the extension directory `cd vscode/microsoft-kiota`.
1. install dependencies `npm i`.
1. pack the extension `vsce package`.
1. side load the extension `code --install-extension pathToVsixThatWasCreatedAtPreviousStep` or use the **Install from VSIX** option from the **...** menu in the extensions pane if you're remoting to WSL/devcontainer.

At this point you have the code extension installed, but it needs the kiota executable to work properly.

#### Building kiota

Refer to step 1 from the "debugging" section. The major difference will be the output path:

`~/vscode/extensions/ms-graph.kiota-<versionInPackage.json>/.kiotabin/<versionInPackage.json>/<rid>/`
