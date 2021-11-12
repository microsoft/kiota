---
nav_order: 1
has_children: true
---

# Welcome to Kiota

Kiota is an OpenAPI based code generator for creating SDKs for HTTP APIs. The goal is to produce a lightweight, low maintenance, code generator that is fast enough to run as part of the compile time tool-chain but scalable enough to handle the largest APIs.

For those looking to try it out rather than hearing why we built it, checkout out the [Get started with Kiota](getstarted.md) section.

Current SDK tooling forces the API provider to make choices about the granularity of how API consumers want to consume their APIs. However you can't please everyone. Some developers building mobile applications care deeply about binary footprint and want SDKs that only contain what they need. Other developers building enterprise experiences don't want have to worry about finding which one in a dozen SDKs contain the feature they are looking for. Many companies are beginning to use API Management gateways and portals to bring APIs across their organization together and provide a coherent and consistent experience across many APIs. However, traditional SDKs continue to be shipped based on the team that provided the API. Kiota has the flexibility to quickly and easily build SDKs the shape and size that our customers need regardless of size. Conway's law doesn't apply here.

## Goals

- Fast and scalable source code generator to simplify calling HTTP APIs
- Provide support for a wide range of languages: C#, Java, Typescript, PHP, Ruby, Go
- Leverage the full capabilities of OpenAPI descriptions
- Enable low effort implementation of new language support
- Generate only the source code necessary by building on a core library
- Minimize external dependencies
- Leverage JSON Schema descriptions to generate primitive based model serialization/deserialization code
- Enable generation of code for only a specified subset of an OpenAPI description
- Generate code that enables IDE autocomplete to aid in API resource discovery
- Enable full access to HTTP capabilities
- Lightweight, easy to install command line tool
- Provide the ability to generate example usage of the SDKs based on HTTP snippets

## Non-Goals

- Extensibility model for creating different SDK API shapes
- Support for other API description formats

## Where next

To discover more about the developer experience that Kiota SDKs could bring to your customers, read about the [experience](experience.md).

For details about how to use Kiota to generate SDKs, read the [using](using.md) section.

To gain a better understanding of how Kiota works and how to extend it for other languages read the [extending](extending.md).
