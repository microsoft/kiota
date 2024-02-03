# kiota plugin remove

## Description

`kiota plugin remove` allows a developer to remove an existing plugin from the `kiota-config.json` file. The command will remove the entry from the `plugins` section of `kiota-config.json` file. The command has a single required parameters; the name of the plugin. 

The command also has one optional parameter, the ability to remove the generated plugin. If provided, kiota will delete the folder and its content specified at the `outputPath` from the plugin configuration. It will also remove the local version of the OpenAPI description file (specified by the `x-ms-kiotaHash` property in the API Manifests). The API Manifests are also updated to remove the dependency from the list of dependencies.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--plugin-name \| --cn` | Yes | GitHub | Name of the plugin |
| `--clean-output \| --co` | No |  | Cleans the generated plugin |

#### Using kiota plugin remove

```bash
kiota plugin remove --plugin-name "GitHub" --clean-output
```

The resulting `kiota-config.json` file will look like this:

```jsonc
{
  "version": "1.0.0",
  "plugins": { }
}
```

_The resulting `github-apimanifest.json` file will look like this:_

```jsonc
{
  "apiDependencies": { }
}
```

_The resulting `apimanifest.json` file (concatenated surface of all APIs) will look like this:_

```jsonc
{
  "apiDependencies": {
    "GraphPlugin": {
      "x-ms-kiotaHash": "1GFCD345RF3DD98...",
      "apiDescriptionUrl": "https://aka.ms/graph/v1.0/openapi.yaml",
      "apiDeploymentBaseUrl": "https://graph.microsoft.com",
      "apiDescriptionVersion": "v1.0",
      "requests": [ ... ]
    }
  }
} //GitHub plugin was removed
```

## File structure
```bash
/
 └─.kiota
    └─plugins
       └─GraphPlugin.yaml
 └─generated
    └─plugins
       └─graphplugin
          └─graphplugin-typeb.json
          └─graphplugin-apimanifest.json
       └─github
 └─kiota-config.json
 └─apimanifest.json
```