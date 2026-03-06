# Contributing to kiota npm package

Thanks for your interest in contributing to Kiota npm package! We welcome contributions from everyone, regardless of skill level or experience. See [general contribution guidelines](../../CONTRIBUTING.md) for general contribution guidelines.

## Getting Started

To get started, you'll need to have the following tools installed:

- [.NET SDK 10.0](https://get.dot.net/10)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Node.js](https://nodejs.org/en/download/) (LTS version)

## Building the project

```sh
npm run build
```

## Running the tests

### Build and publish the Kiota generator

Context: you're a developer working on the extension, and you want to run tests including the integration ones.

```sh
dotnet restore ../../src/kiota
dotnet publish ../../src/kiota/kiota.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained -f net10.0 -c Debug -r <rid> -o ./vscode/npm-package/.kiotabin/0.0.1/<rid>/
```

where rid is one of `win-x64|linux-x64|osx-x64`.

This will create a folder in `vscode/npm-package/.kiotabin/0.0.1/<rid>/` with the Kiota executable that will be used for the integration tests.

### Run the tests

```sh
npm test
```

 
### Debugging the Kiota npm package

See [Debugging the package](https://github.com/microsoft/kiota/blob/main/vscode/npm-package/debugging.md)
