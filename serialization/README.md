# Welcome to the Kiota Serialization section

The Kiota Serialization libraries are language specific libraries implementing the serialization interfaces required by Kiota projects for diverse formats.
Your project will need a reference to the abstraction package to build and run, the following languages are currently supported:

## Application/json

- [Dotnet](https://github.com/microsoft/kiota-serialization-json-dotnet): relies on [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=net-6.0) for JSON serialization/deserialization.
- [Go](./go/json): relies on [encoding/json](https://pkg.go.dev/encoding/json) for JSON serialization/deserialization.
- [Java](./java/json) : relies on [Gson](https://github.com/google/gson) for JSON serialization/deserialization.
- [TypeScript](./typescript/json) : relies on the native JSON capabilities for JSON serialization/deserialization.
- [PHP](./php/json) : relies on the native JSON capabilities for JSON deserialization

## Text/plain

- [Dotnet](https://github.com/microsoft/kiota-serialization-text-dotnet)
- [Go](./go/text)
- [Java](./java/text)
- [TypeScript](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/text)
