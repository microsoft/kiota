# login

## Description 

Use `kiota login` to sign in to private repositories and search for/display/generate clients for private API descriptions. This command makes sub-commands available to sign in to specific authentication providers.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `search-provider` | Yes | github | The search provided to login with. | Yes |
| `type` | Yes | device | The authentication strategy to use. | Yes |
| `--log-level \| --ll` | No | Critical | The log level to use when logging messages to the main output. Options available: Critical, Debug, Error, Information, None, Trace & Warning. Defaults to `Warning`. | Yes |
| `--pat` | No | PAT value | The PAT used to login | Yes, without its value |

## Usage

```bash
kiota login github device
```