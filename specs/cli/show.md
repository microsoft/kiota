# show

## Description 

Show the API paths tree for an API description.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `--openapi \| -d` | Yes | https://aka.ms/graph/v1.0/openapi.yaml | The location of the OpenAPI description in JSON or YAML format to use to generate the SDK. Accepts a URL or a local path. | Yes, without its value |
| `--clear-cache \| --cc` | No | true | Clears any cached data for the current command. Defaults to `False`. | Yes |
| `--log-level \| --ll` | No | Critical | The log level to use when logging messages to the main output. Options available: Critical, Debug, Error, Information, None, Trace & Warning. Defaults to `Warning`. | Yes |
| `--version \| --v` | No | beta | The version of the OpenAPI document to use | Yes, without its value |
| `--search-key \| --k` | No | github::microsoftgraph/msgraph-metadata/graph.microsoft.com/v1.0 | The search key used to locate the OpenAPI description. | Yes, without its value |
| `--max-depth \| --m-d` | No | 10 | The maximum depth of the tree to display. Defaults to `5`. | Yes, without its value |
| `--include-path \| -i` | No | /me/chats#GET | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. | Yes, without its value |
| `--exclude-path \| -e` | No | \*\*/users/\*\* | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. | Yes, without its value |

## Usage

```bash
kiota show -d https://aka.ms/graph/v1.0/openapi.yaml -i **/messages

```

```bash
/
 └─users
    └─{user-id}
       ├─mailFolders
       │  └─{mailFolder-id}
       │     ├─childFolders
       │     └─messages
       └─messages
```