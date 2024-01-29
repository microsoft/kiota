# kiota plugin add

## Description 

`kiota plugin add` allows a developer to add a new manifest to the `kiota-plugin-config.json` file. If no `kiota-plugin-config.json` file is found, a new `kiota-plugin-config.json` file would be created in the current working directory. The command will add a new entry to the `plugins` section of the `kiota-plugin-config.json` file. Once this is done, a local copy of the OpenAPI description is generated and kept in the `.kiota/plugins/descriptions` folder. If a plugin with the same name already exists, the command will fail and display an actionnable error message.

When executing, a new plugin entry will be added and will use the `--plugin-name` parameter as the key for the map. When loading the OpenAPI description, it will store the location of the description in the `descriptionLocation` property. If `--include-path` or `--exclude-path` are provided, they will be stored in the `includePatterns` and `excludePatterns` properties respectively.

Every time a plugin is added, a copy of the OpenAPI description file will be stored in the `./.kiota/plugins/{plugin-name}` folder. The files will be named using the plugin name. This will allow the CLI to detect changes in the description and avoid downloading the description again if it hasn't changed.

Developers can generate `typea`, `typeb` and `apimanifest`. By generating `typea` or `typeb`, an [API Manifest](https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html#section-2.5-3) will also be generated and the file will be names `apimanifest.json`. `apimanifest` will generate only an API Manifest.

Once the `kiota-plugin-config.json` file is generated and the OpenAPI description file is saved locally, the generation will be executed and the manifest will become available.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--plugin-name \| --pn` | Yes | GitHub | Name of the plugin. Unique within the parent API. Defaults to `Plugin` |
| `--openapi \| -d` | Yes | https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json | The location of the OpenAPI description in JSON or YAML format to use to generate the manifest. Accepts a URL or a local path. |
| `--include-path \| -i` | No | /issues/\*\* | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. |
| `--exclude-path \| -e` | No | /advisories | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. |
| `--type \| -t` | Yes | typea | The target type of manifest for the generated code files or for the information. Possible values are `typea`, `typeb` and `apimanifest`. Defaults to `apimanifest`|
| `--overlayPath \| --op` | No | ./overlay/plugins/github/overlay.yaml | The location of the overlay file in JSON or YAML format to be used to generate the plugin. [Overlay](https://github.com/OAI/Overlay-Specification/blob/main/versions/1.0.0.md) defines a way of creating documents that contain additional information to be merged with an OpenAPI description. Defaults to no value which uses the OpenAPI description as it is. |
| `--skip-generation \| --sg` | No | true | When specified, the generation would be skipped. Defaults to false. |
| `--output \| -o` | No | ./generated/plugins/github | The output directory or file path for the generated code files. This is relative to the location of `kiota-plugin-config.json`. Defaults to `./output`. |

> [!NOTE] 
> It is not required to use the CLI to add new plugins. It is possible to add a new plugins by adding a new entry in the `plugins` section of the `kiota-plugin-config.json` file. See the [kiota-plugin-config.json schema](../schemas/kiota-plugin-config.json) for more information. Using `kiota plugin generate --plugin-name myPlugin` would be required to generate the manifests.

## Using `kiota plugin add`

```bash
kiota plugin add --plugin-name "GitHub" --openapi "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json" --include-path "/issues/**" --type typea --output "./generated/plugins/github"
```

_The resulting `kiota-plugin-config.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
  "plugins": {
    "GitHub": {
      "descriptionLocation": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
      "type": "typea",
      "includePatterns": ["/issues/**"],
      "excludePatterns": [],
      "outputPath": "./generated/plugins/github",
      "overlayPath": "./overlays/plugins/github/overlay.yaml"
    }
  }
}
```

_The resulting `typea.json` file will look like this:_

```jsonc

```

_The resulting `apimanifest.json` file will look like this:_

```jsonc
{
  "apiDependencies": {
    "GraphClient": {
      "x-ms-kiotaHash": "9EDF8506CB74FE44...",
      "apiDescriptionUrl": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
      "apiDeploymentBaseUrl": "https://api.github.com/",
      "apiDescriptionVersion": "v1.0",
      "requests": [
        {
          "method": "GET",
          "uriTemplate": "/issues"
        }
      ]
    }
  }
}
```

## File structure
```bash
/
 └─.kiota
    └─definitions
       └─GitHub.json
 └─generated
    └─plugins
       └─github
          └─typea.json
          └─apimanifest.json
 └─kiota-plugin-config.json
```
