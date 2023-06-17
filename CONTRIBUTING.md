# Contributing to Kiota

Thanks for your interest in contributing to Kiota! We welcome contributions from everyone, regardless of skill level or experience. Here are some guidelines to help you get started:

## Getting Started

To get started, you'll need to have the following tools installed:

- [.NET SDK 7.0](https://get.dot.net/7)

## Recommended tools

- [Visual Studio Code](https://code.visualstudio.com/)
- [Microsoft Graph PowerShell SDK](https://github.com/microsoftgraph/msgraph-sdk-powershell), cloned into the same parent folder of this repository. This dependency is only required if you want to generate SDKs for Microsoft Graph.
- [reportgenerator](https://www.nuget.org/packages/dotnet-reportgenerator-globaltool), if you want to be able to generate coverage reports from the pre-configured visual studio code test tasks.

## Building the project

```sh
dotnet restore ./src/kiota
dotnet build ./src/kiota
```

## Running the tests

### Test the kiota command line


```sh
dotnet test ./tests/Kiota.Tests/
```

### Test the kiota builder

```sh
dotnet test ./tests/Kiota.Builder.Tests
```

## Contributing Code

1. Fork the repository and clone it to your local machine.
2. Create a new branch for your changes: `git checkout -b my-new-feature`
3. Make your changes and commit them: `git commit -am 'Add some feature'`
    - Include tests that cover your changes.
    - Update the documentation to reflect your changes, where appropriate.
    - Add an entry to the `CHANGELOG.md` file describing your changes if appropriate.
4. Push your changes to your fork: `git push origin my-new-feature`
5. Create a pull request from your fork to the main repository.

## Troubleshooting

### Build errors

If you get build errors, particularly a message like this:
```
CSC : error CS9057: The analyzer assembly '<path>/KiotaGenerated.dll' references version '4.5.0.0' of the compiler, which is newer than the currently running version '4.4.0.0'. 
```

try updating your .NET SDK to the latest version.

## Reporting Bugs

If you find a bug in Kiota, please report it by opening a new issue in the issue tracker. Please include as much detail as possible, including steps to reproduce the bug and any relevant error messages.

## License

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit [https://cla.opensource.microsoft.com](https://cla.opensource.microsoft.com).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
