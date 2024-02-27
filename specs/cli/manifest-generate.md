# kiota manifest generate

## Description

Now that we have a `kiota-config.json` file, all the parameters required to generate the plugin manifests are stored in the file. The `kiota manifest generate` command will read the `kiota-config.json` file and generate the output files for each of the available plugins. 

It's also possible to specify for which plugin manifest the output files should be generated. This is useful when there are multiple plugin manifests. The `kiota manifest generate --plugin-name "MyPluginManifest"` command will read the `kiota-config.json` file and generate the code for the specified plugin manifest. If it can't find the specified plugin manifest, it will throw an error.

In general cases, the `kiota manifest generate` command will generate the outfiles for all the plugin manifests in the `kiota-config.json` file based on the cached OpenAPI description. If the `--refresh` parameter is provided, the command will refresh the cached OpenAPI description(s), update the different `x-ms-kiotaHash` in the API Manifests and then generate the output files for the specified plugins.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--plugin-name \| --cn` | No | GitHub | Name of the plugin. Unique within the parent API. |
| `--refresh \| -r` | No | true | Provided when refreshing the description(s) is required. |

## Usage

### Using `kiota manifest generate` for a single plugin manifest

```bash
kiota manifest generate --plugin-name "GitHub"
```

### Using `kiota manifest generate` for all plugin manifests

```bash
kiota manifest generate
```