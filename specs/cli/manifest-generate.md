# kiota manifest generate

## Description

Now that we have a `kiota-config.json` file, all the parameters required to generate the manifests are stored in the file. The `kiota manifest generate` command will read the `kiota-config.json` file and generate the output files for each of the available manifests. 

It's also possible to specify for which manifest the output files should be generated. This is useful when there are multiple plugin manifests. The `kiota manifest generate --manifest-name "MyManifest"` command will read the `kiota-config.json` file and generate the output files for the specified manifest. If it can't find the specified manifest, it will throw an error.

In general cases, the `kiota manifest generate` command will generate the output files for all the manifests in the `kiota-config.json` file based on the cached OpenAPI description. If the `--refresh` parameter is provided, the command will refresh the cached OpenAPI description(s), update the different `x-ms-kiotaHash` in the API Manifests and then generate the output files for the specified manifests.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `--manifest-name \| --mn` | No | GitHub | Name of the manifest. Unique within the parent API. | Yes, without its value |
| `--refresh \| -r` | No | true | Provided when refreshing the description(s) is required. | Yes |

## Usage

### Using `kiota manifest generate` for a single manifest

```bash
kiota manifest generate --manifest-name "GitHub"
```

### Using `kiota manifest generate` for all manifests

```bash
kiota manifest generate
```