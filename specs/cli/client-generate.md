# kiota client generate

## Description

Now that we have a `kiota-config.json` file, all the parameters required to generate the code are stored in the file. The `kiota client generate` command will read the `kiota-config.json` file and generate the code for each of the available clients. 

It's also possible to specify for which API and client the code should be generated. This is useful when a project contains multiple clients. The `kiota client generate --client-name "MyClient"` command will read the `kiota-config.json` file and generate the code for the specified client. If it can't find the specified API or client, it will throw an error.

In general cases, the `kiota client generate` command will generate the code for all the clients in the `kiota-config.json` file based on the cached OpenAPI description. If the `--refresh` parameter is provided, the command will refresh the cached OpenAPI description(s), update the different `descriptionHash` and then generate the code for the specified clients.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--client-name \| --cn` | No | graphDelegated | Name of the client. Unique within the parent API. |
| `--refresh \| -r` | No | true | Provided when refreshing the description(s) is required. |

## Usage

### Using `kiota client generate` for a single API client

```bash
kiota client generate --client-name "graphDelegated"
```

### Using `kiota client generate` for all API clients

```bash
kiota client generate
```

### Using `kiota client generate` for all API clients and refresh their descriptions

```bash
kiota client generate
```
