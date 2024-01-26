# logout

## Description 

Use kiota logout to logout from a private repository containing API descriptions.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- |
| `search-provider` | Yes | github | The search provided to login with. | Yes |
| `--log-level \| --ll` | No | Critical | The log level to use when logging messages to the main output. Options available: Critical, Debug, Error, Information, None, Trace & Warning. Defaults to `Warning`. | Yes |

## Usage

```bash
kiota logout github
```