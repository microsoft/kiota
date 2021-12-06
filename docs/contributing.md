---
parent: Welcome to Kiota
nav_order: 4
---

# Contributing to Kiota

## Required tools

- [.NET SDK 6.0](https://dotnet.microsoft.com/download)

## Recommended tools

- [Visual Studio Code](https://code.visualstudio.com/)
- [Microsoft Graph PowerShell SDK](https://github.com/microsoftgraph/msgraph-sdk-powershell), cloned into the same parent folder of this repository. This dependency is only required if you want to generate SDKs for Microsoft Graph.
- [reportgenerator](https://www.nuget.org/packages/dotnet-reportgenerator-globaltool), if you want to be able to generate coverage reports from the pre-configured visual studio code test tasks.

## Repository initialization

This repository makes use of git submodules, after cloning the repository, run the following commands

```shell
git submodule init
git submodule update --remote --merge
```
