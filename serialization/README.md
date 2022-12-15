# Welcome to the Kiota Serialization section

The Kiota Serialization libraries are language specific libraries implementing the serialization interfaces required by Kiota projects for diverse formats.
Your project will need a reference to the abstraction package to build and run, the following languages are currently supported:

## Application/json

- [Dotnet](https://github.com/microsoft/kiota-serialization-json-dotnet): relies on [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=net-7.0) for JSON serialization/deserialization.
- [Go](https://github.com/microsoft/kiota-serialization-json-go): relies on [encoding/json](https://pkg.go.dev/encoding/json) for JSON serialization/deserialization.
- [Java](https://github.com/microsoft/kiota-java/tree/main/components/serialization/json) : relies on [Gson](https://github.com/google/gson) for JSON serialization/deserialization.
- [TypeScript](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/json) : relies on the native JSON capabilities for JSON serialization/deserialization.
- [PHP](https://github.com/microsoft/kiota-serialization-json-php) : relies on the native JSON capabilities for JSON deserialization
- [Python](https://github.com/microsoft/kiota-serialization-json-python) : relies on the native JSON capabilities.

## Application/x-www-form-urlencoded

- [Dotnet](https://github.com/microsoft/kiota-serialization-form-dotnet).
- [Go](https://github.com/microsoft/kiota-serialization-form-go).
- [Java](https://github.com/microsoft/kiota-java/tree/main/components/serialization/form).
- [TypeScript](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/form).

## Text/plain

- [Dotnet](https://github.com/microsoft/kiota-serialization-text-dotnet)
- [Go](https://github.com/microsoft/kiota-serialization-text-go)
- [Java](https://github.com/microsoft/kiota-java/tree/main/components/serialization/text)
- [TypeScript](https://github.com/microsoft/kiota-typescript/tree/main/packages/serialization/text)
- [PHP](https://github.com/microsoft/kiota-serialization-text-php)
- [Python](https://github.com/microsoft/kiota-serialization-text-python)
