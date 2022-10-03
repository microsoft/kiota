---
parent: Welcome to Kiota
nav_order: 2
---

# Using the Kiota tool

<!-- markdownlint-disable MD024 -->

> **Note:** For information on installing Kiota, see [Get started with Kiota](get-started/index.md).

## Commands

Kiota offers the following commands to help you build your API client:

- **[search](#description-search)**: search for APIs and their description from various registries.
- **[download](#description-download)**: download an API description.
- **[generate](#client-generation)**: generate a client for any API from its description.

## Description search

Kiota search accepts the following parameters while searching for APIs and their descriptions.

```shell
kiota search <searchTerm>
      [(--clear-cache | --cc)]
      [(--log-level | --ll) <level>]
      [(--version | -v) <version>]
```

> Note: the search command requires access to internet and cannot be used offline.

### Mandatory arguments

#### Search term

The term to use during the search for APIs and their descriptions.

If multiple results are found, kiota will present a table with the different results.

The following example returns multiple results.

```shell
kiota search github
```

Which will return the following display.

```shell
-----------------------------------------------------------------------------------------------
 | key                                 | title              | description           | versions |
 -----------------------------------------------------------------------------------------------
 | apisguru::github.com                | GitHub v3 REST API | GitHub's v3 REST API. | 1.1.4    |
 -----------------------------------------------------------------------------------------------
 | apisguru::github.com:api.github.com | GitHub v3 REST API | GitHub's v3 REST API. | 1.1.4    |
 -----------------------------------------------------------------------------------------------
 | apisguru::github.com:ghes-2.18      | GitHub v3 REST API | GitHub's v3 REST API. | 1.1.4    |
 -----------------------------------------------------------------------------------------------
 | apisguru::github.com:ghes-2.19      | GitHub v3 REST API | GitHub's v3 REST API. | 1.1.4    |
 -----------------------------------------------------------------------------------------------
 | apisguru::github.com:ghes-2.20      | GitHub v3 REST API | GitHub's v3 REST API. | 1.1.4    |
 -----------------------------------------------------------------------------------------------
 | apisguru::github.com:ghes-2.21      | GitHub v3 REST API | GitHub's v3 REST API. | 1.1.4    |
 -----------------------------------------------------------------------------------------------
 | apisguru::github.com:ghes-2.22      | GitHub v3 REST API | GitHub's v3 REST API. | 1.1.4    |
 -----------------------------------------------------------------------------------------------
 | apisguru::github.com:ghes-3.0       | GitHub v3 REST API | GitHub's v3 REST API. | 1.1.4    |
 -----------------------------------------------------------------------------------------------
 | apisguru::github.com:ghes-3.1       | GitHub v3 REST API | GitHub's v3 REST API. | 1.1.4    |
 -----------------------------------------------------------------------------------------------
```

If the search term is an exact match with one of the results' key, the search command will display a detailed view of the result.

```shell
kiota search apisguru::github.com
```

```shell
Key: apisguru::github.com
Title: GitHub v3 REST API
Description: GitHub's v3 REST API.
Service: https://support.github.com/contact
OpenAPI: https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json
```

### Optional parameters

The search command accepts optional parameters commonly available on the other commands:

- **[--clear-cache](#clear-cache---co)**
- **[--log-level](#log-level---ll)**
- **[--version](#version--v)**

## Description download

Kiota download downloads API descriptions to a local from a registry and accepts the following parameters.

It is not mandatory to download the description locally for the purpose of generation as the generation command can download the description directly. However, having a local copy of the description can be helpful to inspect it, determine which API paths are needed, and come up with the include/exclude filters for generation.

> Note: the download command requires access to internet and cannot be used offline.

```shell
kiota download <searchTerm>
      [--clear-cache | --cc]
      [--clean-output | --co]
      [(--log-level | --ll) <level>]
      [(--version | -v) <version>]
      [(--output | -o) <path>]
```

### Mandatory arguments

#### Search term

The search term to use to locate the description. The description will be downloaded only if an exact key match occurs. You can use the search command to find the key of the API description before downloading it.

```shell
kiota download apisguru::github.com
```

### Optional parameters

The download command accepts optional parameters commonly available on the other commands:

- **[--clean-output](#clean-output---co)**
- **[--clear-cache](#clear-cache---co)**
- **[--log-level](#log-level---ll)**
- **[--output](#output--o)**
- **[--version](#version--v)**

## Client generation

Kiota generate accepts the following parameters during the generation.

```shell
kiota generate (--openapi | -d) <path>
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
      [--clear-cache | --cc]
      [(--structured-mime-types | -m) <mime-types>]
      [(--include-path | -i) <glob pattern>]
      [(--exclude-path | -e) <glob pattern>]
```

### Mandatory parameters

#### `--openapi (-d)`

The location of the OpenAPI description in JSON or YAML format to use to generate the SDK. Accepts a URL or a local path.

#### `--language (-l)`

The target language for the generated code files.

##### Accepted values

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
kiota generate --language java
```

### Optional parameters

The generate command accepts optional parameters commonly available on the other commands:

- **[--clear-cache](#clear-cache---co)**
- **[--clean-output](#clean-output---co)**
- **[--log-level](#log-level---ll)**
- **[--output](#output--o)**

#### `--backing-store (-b)`

Enables backing store for models. Defaults to `false`.

```shell
kiota generate --backing-store
```

#### `--additional-data (--ad)`

Will include the 'AdditionalData' property for generated models. Defaults to 'true'.

```shell
kiota generate --additional-data false
```

#### `--class-name (-c)`

The class name to use for the core client class. Defaults to `ApiClient`.

##### Accepted values

The provided name MUST be a valid class name for the target language.

```shell
kiota generate --class-name MyApiClient
```

#### `--deserializer (--ds)`

The fully qualified class names for deserializers. Defaults to the following values.

| Language   | Default deserializers                                           |
|------------|-----------------------------------------------------------------|
| C#         | `Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory`, `Microsoft.Kiota.Serialization.Text.TextParseNodeFactory`      |
| Go         | `github.com/microsoft/kiota-serialization-json-go/json.JsonParseNodeFactory`, `github.com/microsoft/kiota-serialization-text-go/text.TextParseNodeFactory` |
| Java       | `com.microsoft.kiota.serialization.JsonParseNodeFactory`, `com.microsoft.kiota.serialization.TextParseNodeFactory`        |
| Ruby       | `microsoft_kiota_serialization/json_parse_node_factory`         |
| TypeScript | `@microsoft/kiota-serialization-json.JsonParseNodeFactory`, `@microsoft/kiota-serialization-text.TextParseNodeFactory`      |

##### Accepted values

One or more module names that implements `IParseNodeFactory`.

```shell
kiota generate --deserializer Contoso.Json.CustomDeserializer
```

#### `--exclude-path (-e)`

A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing.

```shell
kiota generate --exclude-path **/users/** --exclude-path **/groups/**
```

> Note: exclude pattern can be used in combination with the include pattern argument. A path item is included when (no include pattern is included OR it matches an include pattern) AND (no exclude pattern is included OR it doesn't match an exclude pattern).

#### `--include-path (-i)`

A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything.

```shell
kiota generate --include-path **/users/** --include-path **/groups/**
```

> Note: include pattern can be used in combination with the exclude pattern argument. A path item is included when (no include pattern is included OR it matches an include pattern) AND (no exclude pattern is included OR it doesn't match an exclude pattern).

#### `--namespace-name (-n)`

The namespace to use for the core client class specified with the `--class-name` option. Defaults to `ApiSdk`.

##### Accepted values

The provided name MUST be a valid module or namespace name for the target language.

```shell
kiota generate --namespace-name MyAppNamespace.Clients
```

##### Accepted values

A valid URI to an OpenAPI description in the local filesystem or hosted on an HTTPS server.

```shell
kiota generate --openapi https://contoso.com/api/openapi.yml
```

#### `--serializer (-s)`

The fully qualified class names for deserializers. Defaults to the following values.

| Language   | Default deserializer                                            |
|------------|-----------------------------------------------------------------|
| C#         | `Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory`, `Microsoft.Kiota.Serialization.Text.TextSerializationWriterFactory` |
| Go         | `github.com/microsoft/kiota-serialization-json-go/json.JsonSerializationWriterFactory`, `github.com/microsoft/kiota-serialization-text-go/text.TextSerializationWriterFactory` |
| Java       | `com.microsoft.kiota.serialization.JsonSerializationWriterFactory`, `com.microsoft.kiota.serialization.TextSerializationWriterFactory` |
| Ruby       | `microsoft_kiota_serialization/json_serialization_writer_factory` |
| TypeScript | `@microsoft/kiota-serialization-json.JsonSerializationWriterFactory`, `@microsoft/kiota-serialization-text.TextSerializationWriterFactory` |

##### Accepted values

One or more module names that implements `ISerializationWriterFactory`.

```shell
kiota generate --serializer Contoso.Json.CustomSerializer
```

#### `--structured-mime-types (-m)`

The MIME types to use for structured data model generation. Accepts multiple values.

Default values :

- `application/json`
- `application/xml`
- `text/plain`
- `text/xml`
- `text/yaml`

> Note: Only request body types or response types with a defined schema will generate models, other entries will default back to stream/byte array.

##### Accepted values

Any valid MIME type which will match a request body type or a response type in the OpenAPI description.

### Examples

```shell
kiota generate --structured-mime-types application/json
```

## Common parameters

The following parameters are available across multiple commands.

### Optional parameters

#### `--clean-output (--co)`

Delete the output directory before generating the client. Defaults to false.

Available for commands: download, generate.

```shell
kiota <command name> --clean-output
```

#### `--clear-cache (--co)`

Clears the currently cached file for the command. Defaults to false.

Cached files are stored under `%TEMP%/kiota/cache` and valid for one (1) hour after the initial download. Kiota caches API descriptions during generation and static index files during search.

```shell
kiota <command name> --clear-cache
```

#### `--log-level (--ll)`

The log level to use when logging events to the main output. Defaults to `warning`.

Available for commands: all.

##### Accepted values

- `critical`
- `debug`
- `error`
- `information`
- `none`
- `trace`
- `warning`

```shell
kiota <command name> --log-level information
```

#### `--output (-o)`

The output directory or file path for the generated code files. Defaults to `./output` for generate and `./output/result.json` for download.

Available for commands: download, generate.

##### Accepted values

A valid path to a directory (generate) or a file (download).

```shell
kiota <command name> --output ./src/client
```

#### `--version (-v)`

Select a specific version of the API description. No default value.

Available for commands: download, search.

```shell
kiota <command name> --version beta
```

### Adding an SDK to a project

First you will need to [install the tools necessary](get-started/index.md) to build the files generated by Kiota.
