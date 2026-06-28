# Debugging the extension

## Running in debug mode

Context: you're a developer working on the extension, and you want to debug/step through the extension code.

1. run `dotnet publish ./src/kiota/kiota.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained -f net10.0 -c Release -r <rid> -o ./vscode/packages/npm-package/.kiotabin/<versionInPackage.json>/<rid>/` where rid is one of `win-x64|linux-x64|osx-x64` and versionInPackage is the Version field in `vscode/packages/npm-package/package.json`.
1. in `vscode` run `npm i && npm run build`.
1. in `vscode/packages/microsoft-kiota` run `code .`
1. in the code instance that was opened at step 2, just press f5

## Side loading the extension

Context: you're most likely a program manager eager to demo the latest bits from an active development branch.

### Pre-requisites

- GitHub CLI `winget install GitHub.CLI`
- dotnet 9 `winget install Microsoft.DotNet.SDK.9`
- node 20 `winget install CoreyButler.NVMforWindows && nvm install lts && nvm use lts`
- vsce & TypeScript `npm i -g typescript @vscode/vsce`

### Steps

#### Building and side-loading the extension

1. make sure to uninstall any version of the extension, including from remote environments if you're remoting into WSL or a devcontainer, to clear any confusion.
1. clone this repository if you don't have it locally already `gh repo clone microsoft/kiota && cd kiota`.
1. switch to the development branch `git switch branchName` (you can find the branch name in the pull request).
1. cd into the extension directory `cd vscode/packages/microsoft-kiota`.
1. install dependencies `npm i`.
1. pack the extension `vsce package`.
1. side load the extension `code --install-extension pathToVsixThatWasCreatedAtPreviousStep` or use the **Install from VSIX** option from the **...** menu in the extensions pane if you're remoting to WSL/devcontainer.

At this point you have the code extension installed, but it needs the kiota executable to work properly.

> Note: alternatively to building the extension yourself, you can download it from [the pipeline](https://github.com/microsoft/kiota/actions/workflows/build-vscode-extension.yml) by filtering the branch (top right corner) with the pull request branch, selecting the latest run, and downloading the vsix from the artifacts. This will allow you to jump directly to the last step.

#### Building kiota

Refer to step 1 from the "debugging" section. The major difference will be the output path:

`~/.vscode/extensions/ms-graph.kiota-<versionInPackage.json>/.kiotabin/<versionInPackage.json>/<rid>/`

Where kiotaVersionInPackage is the kiotaVersion field and versionInPackage is the version field in package.json.

> Note: the **.vscode** segment might change to **.vscode-server** if you're remoting to WSL.

> Note: alternatively to building executable yourself, you can download it from [the pipeline](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml) by filtering the branch (top right corner) with the pull request branch, selecting the latest run, and downloading the right OS version from the artifacts. The only remaining work will be to move the downloaded files to the right path above.

#### Using the ready-made script

If you want to automate these steps, you can use the following script to automate the process:

```powershell
.\scripts\execute-vscode-sideload.ps1 -Platform "win-x64|linux-x64|osx-x64" 
```

If you are using WSL, you can use the following:

```powershell
.\scripts\execute-vscode-sideload.ps1 -WSL
```

## FAQ

### VS Code keeps reinstalling from the marketplace

This is most likely caused by the fact that there's a newer released version. Run the following script replacing the version argument with the latest version of kiota.

```powershell
.\scripts\update-vscode-releases.ps1 -version "versionWithoutVPrefix" -online -filePath .\vscode\microsoft-kiota\package.json
```
