# kiota manifest remove

## Description

`kiota manifest remove` allows a developer to remove an existing manifest from the `kiota-config.json` file. The command will remove the entry from the `manifests` section of `kiota-config.json` file. The command has a single required parameters; the name of the manifest. 

The command also has one optional parameter, the ability to remove the all generated files. If provided, kiota will delete the folder and its content specified at the `outputPath` from the manifest configuration. It will also remove the local version of the OpenAPI description file (specified by the `x-ms-kiotaHash` property in the API Manifests). The API Manifests are also updated to remove the dependency from the list of dependencies.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--manifest-name \| --cn` | Yes | GitHub | Name of the manifest |
| `--clean-output \| --co` | No |  | Cleans the generated manifest files |

#### Using kiota manifest remove and deleting all the content

```bash
kiota manifest remove --manifest-name "GitHub" --clean-output
```
_The resulting `github-apimanifest.json` and `github-openai.json` files will be deleted._

The resulting `kiota-config.json` file will look like this:

```jsonc
{
  "version": "1.0.0",
  "manifests": { }
}
```

_The resulting `apimanifest.json` file (concatenated surface of all APIs) will look like this:_

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
} //GitHub manifest was removed
```

## File structure
```bash
/
 └─.kiota
 └─generated
    └─manifests
       └─github
 └─kiota-config.json
 └─apimanifest.json
```