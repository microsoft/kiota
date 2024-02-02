# kiota plugin add

## Description 

`kiota plugin add` allows a developer to add a new manifest to the `kiota-config.json` file. If no `kiota-config.json` file is found, a new `kiota-config.json` file would be created in the current working directory. The command will add a new entry to the `plugins` section of the `kiota-config.json` file. Once this is done, a local copy of the OpenAPI description is generated and kept in the `.kiota/plugins` folder. If a plugin with the same name already exists, the command will fail and display an actionnable error message.

When executing, a new plugin entry will be added and will use the `--plugin-name` parameter as the key for the map. When loading the OpenAPI description, it will store the location of the description in the `descriptionLocation` property. If `--include-path` or `--exclude-path` are provided, they will be stored in the `includePatterns` and `excludePatterns` properties respectively.

Every time a plugin is added, a copy of the OpenAPI description file will be stored in the `./.kiota/plugins` folder. The OpenAPI will be named using the plugin name `{plugin-name}.json|yaml`. This will allow the CLI to detect changes in the description and avoid downloading the description again if it hasn't changed.

An [API Manifest][def] file named `apimanifest-plugins` will be generated (if non existing) or updated (if already existing) in the root folder next to `kiota-config.json`. This file will represent a concatenated surface of all APIs used across plugins. A new hash composed of the Kiota version, the OpenAPI description location and the properties of the plugins will be generated and would trigger an update to the [API Manifest][def].

Developers can generate `typea`, `typeb` and `apimanifest`. By generating `typea` or `typeb`, two outputs will be generated: a\) the manifest type you have choosen that will be named `{plugin-name}-{type}.json` and b\) an [API Manifest][def] which will be named `{plugin-name}-apimanifest.json` that will include only the information for this specific plugin. `apimanifest` will generate only an API Manifest.
> [!NOTE] 
> There will be two different [API Manifests][def]. One in the root folder representing a concatenated surface of all APIs and a second one specific to each plugin saved in the choosen output directory.

Once the `kiota-config.json` file is generated and the OpenAPI description file is saved locally, the generation will be executed and the manifest will become available.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--plugin-name \| --pn` | Yes | GitHub | Name of the plugin. Unique within the parent API. Defaults to `Plugin` |
| `--openapi \| -d` | Yes | https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json | The location of the OpenAPI description in JSON or YAML format to use to generate the manifest. Accepts a URL or a local directory. |
| `--include-path \| -i` | No | /issues/\*\* | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. |
| `--exclude-path \| -e` | No | /advisories | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. |
| `--type \| -t` | Yes | typea | The target type of manifest for the generated output files. Possible values are `typea`, `typeb` and `apimanifest`. Defaults to `apimanifest`|
| `--overlayDirectory \| --od` | No | ./overlay/plugins/{plugin-name}/overlay.yaml | The location of the overlay file in JSON or YAML format to be used to generate the plugin. [Overlay](https://github.com/OAI/Overlay-Specification/blob/main/versions/1.0.0.md) defines a way of creating documents that contain additional information to be merged with an OpenAPI description. Defaults to no value which uses the OpenAPI description as it is. |
| `--skip-generation \| --sg` | No | true | When specified, the generation would be skipped. Defaults to false. |
| `--output \| -o` | No | ./generated/plugins/github | The output directory or file path for the generated output files. This is relative to the location of `kiota-config.json`. Defaults to `./output`. |

> [!NOTE] 
> It is not required to use the CLI to add new plugins. It is possible to add a new plugins by adding a new entry in the `plugins` section of the `kiota-config.json` file. See the [kiota-config.json schema](../schemas/kiota-plugin-config.json) for more information. Using `kiota plugin generate --plugin-name myPlugin` would be required to generate the manifests.

## Using `kiota plugin add`

```bash
kiota plugin add --plugin-name "GitHub" --openapi "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json" --include-path "/issues/**" --type typea --output "./generated/plugins/github"
```

_The resulting `kiota-config.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
  "clients": {...}, //if any
  "plugins": {
    "GitHub": {
      "descriptionLocation": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
      "type": "typea",
      "includePatterns": ["/issues/**"],
      "excludePatterns": [],
      "outputDirectory": "./generated/plugins/github",
      "overlayDirectory": "./overlays/plugins/github/overlay.yaml"
    }
  }
}
```

_The resulting `github-typea.json` file will look like this:_

```jsonc

```

_The resulting `github-apimanifest.json` file will look like this:_

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
    └─plugins
       └─GitHub.json
 └─generated
    └─plugins
       └─github
          └─github-typea.json
          └─github-apimanifest.json
 └─kiota-config.json
 └─apimanifest.json
```

[def]: https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html#section-2.5-3