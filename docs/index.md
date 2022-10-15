---
nav_order: 1
has_children: true
---

# Welcome to the Kiota API Client generator

Kiota is a command line tool for generating an API client to call any OpenAPI described API you are interested in. The goal is to eliminate the need to take a dependency on a different API SDK for every API that you need to call. Kiota API clients provide a strongly typed experience with all the features you expect from a high quality API SDK, but without having to learn a new library for every HTTP API.
 
 Kiota is a lightweight and fast code generator that can help you discover, explore and call any HTTP API with minimal effort. Kiota also provides the ability to only generate code for the parts of an API that you care about. The footprint of the generated code is only what you need to make you productive. 

For those looking to try it out rather than hearing why we built it, checkout out the [Get started with Kiota](get-started/index.md) section.

Creating and maintaining SDKs in many languages is expensive for API providers and often results in API consumers being disappointed in the quality of SDKs, or worse, they find their preferred programming language is not supported by the API they need to call. Even when API providers do investing in building high quality SDKs, there is no single agreed upon standard for how HTTP APIs should be exposed in native language libraries.  This translates into more learning for the API consumer.  Many developers who consume APIs have given up on using SDKs and have decided that it is easier for them to use a native HTTP library because of the a consistent experience.  The unfortunate side effect is there are many developers writing generic boilerplate HTTP code to handle HTTP retries, redirects, caching and authorization code.

The lack of high quality, standardized tooling for calling HTTP APIs, has been a factor in some developers choosing to explore other protocol options such as GraphQL and gRPC. Both of these technologies offer generic client side tooling that depends on schema descriptions to make it possible to call any GraphQL or gRPC API with a consistent developer experience. Kiota fills this gap for HTTP APIs using OpenAPI as the API description language.  

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

To gain a better understanding of how Kiota works and how to extend it for other languages read the [extending](extending/index.md).
