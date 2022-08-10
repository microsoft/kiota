---
parent: Welcome to Kiota
nav_order: 2
---

# Using the Kiota tool

<!-- markdownlint-disable MD024 -->

> **Note:** For information on installing Kiota, see [Get started with Kiota](get-started/index.md).

Kiota accepts the following parameters during the generation.

```shell
kiota (--openapi | -d) <path>
      (--language | -l) <language>
      [(--output | -o) <path>]
      [(--class-name | -c) <name>]
      [(--namespace-name | -n) <name>]
      [(--log-level | --ll) <level>]
      [--backing-store | -b]
      [--additional-data | --ad]
      [(--serializer | -s) <classes>]
      [(--deserializer | --ds) <classes>]
      [--clean-output | --co]
      [(--structured-mime-types | -m) <mime-types>]
```

## Mandatory parameters

### `--openapi (-d)`

The location of the OpenAPI description in JSON or YAML format to use to generate the SDK.

### `--language (-l)`

The target language for the generated code files.

#### Accepted values

- `csharp`
- `go`
- `java`
- `php`
- `python`
- `ruby`
- `shell`
- `swift`
- `typescript`

```shell
kiota --language java
```

## Optional parameters

### `--backing-store (-b)`

Enables backing store for models. Defaults to `false`.

```shell
kiota --backing-store
```

### `--additional-data (--ad)`

Will include the 'AdditionalData' property for generated models. Defaults to 'true'.

```shell
kiota --additional-data false
```

### `--class-name (-c)`

The class name to use for the core client class. Defaults to `ApiClient`.

#### Accepted values

The provided name MUST be a valid class name for the target language.

```shell
kiota --class-name MyApiClient
```

### `--clean-output (--co)`

Delete the output directory before generating the client. Defaults to false.

```shell
kiota --clean-output
```

### `--deserializer (--ds)`

The fully qualified class names for deserializers. Defaults to the following values.

| Language   | Default deserializers                                           |
|------------|-----------------------------------------------------------------|
| C#         | `Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory`, `Microsoft.Kiota.Serialization.Text.TextParseNodeFactory`      |
| Go         | `github.com/microsoft/kiota-serialization-json-go/json.JsonParseNodeFactory`, `github.com/microsoft/kiota-serialization-text-go/text.TextParseNodeFactory` |
| Java       | `com.microsoft.kiota.serialization.JsonParseNodeFactory`, `com.microsoft.kiota.serialization.TextParseNodeFactory`        |
| Ruby       | `microsoft_kiota_serialization/json_parse_node_factory`         |
| TypeScript | `@microsoft/kiota-serialization-json.JsonParseNodeFactory`, `@microsoft/kiota-serialization-text.TextParseNodeFactory`      |

#### Accepted values

One or more module names that implements `IParseNodeFactory`.

```shell
kiota --deserializer Contoso.Json.CustomDeserializer
```

### `--log-level (--ll)`

The log level to use when logging events to the main output. Defaults to `warning`.

#### Accepted values

- `critical`
- `debug`
- `error`
- `information`
- `none`
- `trace`
- `warning`

```shell
kiota --loglevel information
```

### `--namespace-name (-n)`

The namespace to use for the core client class specified with the `--class-name` option. Defaults to `ApiSdk`.

#### Accepted values

The provided name MUST be a valid module or namespace name for the target language.

```shell
kiota --namespace-name MyAppNamespace.Clients
```

#### Accepted values

A valid URI to an OpenAPI description in the local filesystem or hosted on an HTTPS server.

```shell
kiota --openapi https://contoso.com/api/openapi.yml
```

### `--output (-o)`

The output directory path for the generated code files. Defaults to `./output`.

#### Accepted values

A valid path to a directory.

```shell
kiota --output ./src/client
```

### `--serializer (-s)`

The fully qualified class names for deserializers. Defaults to the following values.

| Language   | Default deserializer                                            |
|------------|-----------------------------------------------------------------|
| C#         | `Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory`, `Microsoft.Kiota.Serialization.Text.TextSerializationWriterFactory` |
| Go         | `github.com/microsoft/kiota-serialization-json-go/json.JsonSerializationWriterFactory`, `github.com/microsoft/kiota-serialization-text-go/text.TextSerializationWriterFactory` |
| Java       | `com.microsoft.kiota.serialization.JsonSerializationWriterFactory`, `com.microsoft.kiota.serialization.TextSerializationWriterFactory` |
| Ruby       | `microsoft_kiota_serialization/json_serialization_writer_factory` |
| TypeScript | `@microsoft/kiota-serialization-json.JsonSerializationWriterFactory`, `@microsoft/kiota-serialization-text.TextSerializationWriterFactory` |

#### Accepted values

One or more module names that implements `ISerializationWriterFactory`.

```shell
kiota --serializer Contoso.Json.CustomSerializer
```

### `--structured-mime-types (-m)`

The MIME types to use for structured data model generation. Accepts multiple values.

Default values :

- `application/json`
- `application/xml`
- `text/plain`
- `text/xml`
- `text/yaml`

> Note: Only request body types or response types with a defined schema will generate models, other entries will default back to stream/byte array.

#### Accepted values

Any valid MIME type which will match a request body type or a response type in the OpenAPI description.

## Examples

```shell
kiota.exe -d ./mail.yml --language csharp -o ../somepath -n Contoso.ApiClient
```

## Adding an SDK to a project

First you will need to [install the tools necessary](get-started/index.md) to build the files generated by Kiota.
