# kiota plugin generate

## Description

Now that we have a `workspace.json` file, all the parameters required to generate the plugins are stored in the file. The `kiota plugin generate` command will read the `workspace.json` file and generate the output files for each of the available plugins.

It's also possible to specify for which plugin the output files should be generated. This is useful when there are multiple plugin plugins. The `kiota plugin generate --plugin-name "Myplugin"` command will read the `workspace.json` file and generate the output files for the specified plugin. If it can't find the specified plugin, it will throw an error.

In general cases, the `kiota plugin generate` command will generate the output files for all the plugins in the `workspace.json` file based on the cached OpenAPI document. If the `--refresh` parameter is provided, the command will refresh the cached OpenAPI document(s), update the different `x-ms-kiotaHash` in the API plugins and then generate the output files for the specified plugins.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `--plugin-name \| --pn` | No | GitHub | Name of the plugin. Unique within the parent API. | Yes, without its value |
| `--refresh \| -r` | No | true | Provided when refreshing the description(s) is required. | Yes |

## Usage

### Using `kiota plugin generate` for a single plugin

```bash
kiota plugin generate --plugin-name "GitHub"
```

### Using `kiota plugin generate` for all plugins

```bash
kiota plugin generate
```
