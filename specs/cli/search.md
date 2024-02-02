# search

## Description 

Search for APIs and their description from various registries.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `search-term` | Yes | Graph | The term to search for. | Yes, without its value |
| `--clear-cache \| --cc` | No | true | Clears any cached data for the current command. Defaults to `False`. | Yes |
| `--log-level \| --ll` | No | Critical | The log level to use when logging messages to the main output. Options available: Critical, Debug, Error, Information, None, Trace & Warning. Defaults to `Warning`. | Yes |
| `--version \| --v` | No | beta | The version of the OpenAPI document to use | Yes, without its value |

## Usage 

```bash
kiota search github
```

```bash
Key                                  Title               Description            Versions
apisguru::github.com                 GitHub v3 REST API  GitHub's v3 REST API.  1.1.4
apisguru::github.com:api.github.com  GitHub v3 REST API  GitHub's v3 REST API.  1.1.4
apisguru::github.com:ghes-2.18       GitHub v3 REST API  GitHub's v3 REST API.  1.1.4
apisguru::github.com:ghes-2.19       GitHub v3 REST API  GitHub's v3 REST API.  1.1.4
apisguru::github.com:ghes-2.20       GitHub v3 REST API  GitHub's v3 REST API.  1.1.4
apisguru::github.com:ghes-2.21       GitHub v3 REST API  GitHub's v3 REST API.  1.1.4
apisguru::github.com:ghes-2.22       GitHub v3 REST API  GitHub's v3 REST API.  1.1.4
apisguru::github.com:ghes-3.0        GitHub v3 REST API  GitHub's v3 REST API.  1.1.4
apisguru::github.com:ghes-3.1        GitHub v3 REST API  GitHub's v3 REST API.  1.1.4
```

If the search term is an exact match with one of the results' key, the search command will display a detailed view of the result.

```bash
kiota search apisguru::github.com
```

```bash
Key: apisguru::github.com
Title: GitHub v3 REST API
Description: GitHub's v3 REST API.
Service: https://support.github.com/contact
OpenAPI: https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json
```