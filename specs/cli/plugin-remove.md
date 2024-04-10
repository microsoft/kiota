# kiota plugin remove

## Description

`kiota plugin remove` allows a developer to remove an existing plugin from the `workspace.json` file. The command will remove the entry from the `plugins` section of `workspace.json` file. The command has a single required parameters; the name of the plugin. 

The command also has one optional parameter, the ability to remove the all generated files. If provided, kiota will delete the folder and its content specified at the `outputPath` from the plugin configuration. It will also remove the local version of the OpenAPI document file (specified by the `x-ms-kiotaHash` property in the API plugins). The API plugins are also updated to remove the dependency from the list of dependencies.

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `--plugin-name \| --pn` | Yes | GitHub | Name of the plugin | No |
| `--clean-output \| --co` | No |  | Cleans the generated plugin files | Yes |

#### Using kiota plugin remove and deleting all the content

```bash
kiota plugin remove --plugin-name "GitHub" --clean-output
```
_The resulting `github-apimanifest.json`, `ai-plugin.json` and `sliced-github.json` files will be deleted._

The resulting `workspace.json` file will look like this:

```jsonc
{
  "version": "1.0.0",
  "clients": {...}, //if any
  "plugins": { }
}
```

_The resulting `apimanifest.json` file (concatenated surface of all APIs dependencies) in the `./kiota` folder will look like this:_

```jsonc
{
  "apiDependencies": {
    "GraphClient": { //for example, an existing API client for Microsoft Graph
      "x-ms-kiotaHash": "9EDF8506CB74FE44...",
      "apiDescriptionUrl": "https://aka.ms/graph/v1.0/openapi.yaml",
      "apiDeploymentBaseUrl": "https://graph.microsoft.com",
      "apiDescriptionVersion": "v1.0",
      "requests": [ ...]
    }
  }
} //GitHub plugin was removed
```

## File structure
```bash
/
 └─.kiota
    └─workspace.json
    └─apimanifest.json
 └─generated
    └─plugins
```
