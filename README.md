# Project

![Dotnet](https://github.com/microsoft/kiota/actions/workflows/dotnet.yml/badge.svg) ![CodeQL](https://github.com/microsoft/kiota/actions/workflows/codeql-analysis.yml/badge.svg) ![Coverage](./coverage.svg)

Kiota is project to build an OpenAPI based code generator for creating SDKs for HTTP APIs. The goal is to produce a lightweight, low maintenance, code generator that is fast enough to run as part of the compile time tool-chain but scalable enough to handle the largest APIs. Kiota generates a lightweight set of strongly typed classes that layer over a core HTTP library and product an intuitive and discoverable way of creating HTTP requests. A set of abstractions decouple the generated service library from the core allowing a variety of core libraries to be supported.

This library builds on top of the [Microsoft.OpenAPI.NET](https://github.com/microsoft/openapi.net) library to ensure comprehensive support for APIs that use OpenAPI descriptions. One of the goals of the project is to provide the best code generator support possible for OpenAPI and JSON Schema features.

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
