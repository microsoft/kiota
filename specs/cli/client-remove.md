# kiota client remove

## Description

`kiota client remove` allows a developer to remove an existing client from the `kiota.config` file. The command will remove the entry from the `clients` section of `kiota.config` file. The command has a single required parameters; the name of the client. 

The command also has one optional parameter, the ability to remove the generated client. If provided, kiota will delete the folder and its content specified at the `outputPath` from the client configuration. It will also remove the local version of the OpenAPI description file (specified by the `descriptionHash` property). The API Manifest is also updated to remove the dependency from the list of dependencies.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--client-name \| --cn` | Yes | graphDelegated | Name of the client |
| `--clean-output \| --co` | No |  | Cleans the generated client |

#### Using kiota client remove

```bash
kiota client remove --client-name "graphDelegated" --clean-output
```

The resulting `kiota.config` file will look like this:

```json
{
  "version": "1.0.0",
  "clients": { }
}
```

_The resulting `apimanifest.json` file will look like this:_

```json
{
  "publisher": {
    "name": "Microsoft Graph",
    "contactEmail": "graphsdkpub@microsoft.com"
  },
  "apiDependencies": { }
}
```

## File structure
```bash
/
 └─.kiota
    └─kiota.config
    └─apimanifest.json
 └─generated
    └─graph
```