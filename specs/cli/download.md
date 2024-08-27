# download

## Description 

Downloads an API description.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `search-term` | Yes | Graph | The term to search for. | Yes, without its value |
| `--clear-cache \| --cc` | No | true | Clears any cached data for the current command. Defaults to `False`. | Yes |
| `--clear-output \| --cc` | No | true | Delete the output directory before generating the client. Defaults to `False`. | Yes |
| `--log-level \| --ll` | No | Critical | The log level to use when logging messages to the main output. Options available: Critical, Debug, Error, Information, None, Trace & Warning. Defaults to `Warning`. | Yes |
| `--version \| --v` | No | beta | The version of the OpenAPI document to use | Yes, without its value |
| `--output \| --o` | No | beta | The output directory or file path for the generated code files. Defaults `./output/result.json`. | Yes, without its value |

## Usage 

```bash
kiota download apisguru::github.com
```