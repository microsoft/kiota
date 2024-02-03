# kiota plugin edit

## Description 

`kiota plugin update` allows a developer to edit an existing manifest int the `kiota-config.json` file. If either the `kiota-config.json` file or if the `--plugin-name` plugin can't be found within the `kiota-config.json` file, the command should error out and let the developer know.

When executing, the API entry defined by the `--plugin-name` parameter will be modified. All parameters should be supported and the only required one is `--plugin-name`. All others are optional as they would only modify the configuration of the plugin. If the OpenAPI description location changed or any properties of the plugin entry in `kiota-config.json`, a new hash composed of the Kiota version, the OpenAPI description location and the properties of the plugin will be generated and and would trigger an update to the [API Manifest][def] located in the root folder.
> [!NOTE] 
> There will be two different [API Manifests][def]. One in the root folder representing a concatenated surface of all APIs and a second one specific to each plugin saved in the choosen output directory.

Once the `kiota-config.json` file and the API Manifest are updated, the code generation will be executed based on the newly updated API client configuration.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--plugin-name \| --pn` | Yes | GitHub | Name of the plugin. Unique within the parent API. Defaults to `Plugin` |
| `--openapi \| -d` | Yes | https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json | The location of the OpenAPI description in JSON or YAML format to use to generate the manifest. Accepts a URL or a local directory. |
| `--include-path \| -i` | No | /repos/\*\* | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. |
| `--exclude-path \| -e` | No | /repos/{owner}/{repo}/actions/cache/usage | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. |
| `--type \| -t` | Yes | typea | The target type of manifest for the generated output files. Possible values are `typea`, `typeb` and `apimanifest`. Defaults to `apimanifest`|
| `--overlayDirectory \| --od` | No | ./overlay/plugins/{plugin-name}/overlay.yaml | The location of the overlay file in JSON or YAML format to be used to generate the plugin. [Overlay](https://github.com/OAI/Overlay-Specification/blob/main/versions/1.0.0.md) defines a way of creating documents that contain additional information to be merged with an OpenAPI description. Defaults to no value which uses the OpenAPI description as it is. |
| `--skip-generation \| --sg` | No | true | When specified, the generation would be skipped. Defaults to false. |
| `--output \| -o` | No | ./generated/plugins/github | The output directory or file path for the generated output files. This is relative to the location of `kiota-config.json`. Defaults to `./output`. |

> [!NOTE] 
> It is not required to use the CLI to edit plugins. It is possible to edit a plugins by modifying its entry in the `plugins` section of the `kiota-config.json` file. See the [kiota-config.json schema](../schemas/kiota-plugin-config.json) for more information.

## Using `kiota plugin edit`

```bash
kiota plugin edit --plugin-name "GitHub" --exclude-path "/repos/{owner}/{repo}/actions/cache/usage"
```

_The resulting `kiota-config.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
  "clients": {...}, //if any
  "plugins": {
    "GitHub": {
      "descriptionLocation": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
      "includePatterns": ["/repos/**"],
      "excludePatterns": ["/repos/{owner}/{repo}/actions/cache/usage"],
      "type": "typea",
      "outputDirectory": "./generated/plugins/github",
      "overlayDirectory": "./overlays/plugins/github/overlay.yaml"
    }
  }
}
```

_The resulting `github-apimanifest.json` file will look like this:_

```jsonc
{
  "apiDependencies": {
    "GitHub": {
      "x-ms-kiotaHash": "9EDF8506CB74FE44...",
      "apiDescriptionUrl": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
      "apiDeploymentBaseUrl": "https://api.github.com/",
      "apiDescriptionVersion": "v1.0",
      "requests": [
        {
          "method": "GET",
          "uriTemplate": "/repos/{owner}/{repo}"
        },
        {
          "method": "PATCH",
          "uriTemplate": "/repos/{owner}/{repo}"
        },,
        {
          "method": "DELETE",
          "uriTemplate": "/repos/{owner}/{repo}"
        },
        {
          "method": "GET",
          "uriTemplate": "/repos/{owner}/{repo}/actions/artifacts"
        },
        {
          "method": "GET",
          "uriTemplate": "/repos/{owner}/{repo}/actions/artifacts/{artifact_id}"
        },
        {
          "method": "DELETE",
          "uriTemplate": "/repos/{owner}/{repo}/actions/artifacts/{artifact_id}"
        }
      ]
    }
  }
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
    },
    "GitHub": {
      "x-ms-kiotaHash": "9EDF8506CB74FE44...",
      "apiDescriptionUrl": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
      "apiDeploymentBaseUrl": "https://api.github.com/",
      "apiDescriptionVersion": "v1.0",
      "requests": [
        {
          "method": "GET",
          "uriTemplate": "/repos/{owner}/{repo}"
        },
        {
          "method": "PATCH",
          "uriTemplate": "/repos/{owner}/{repo}"
        },,
        {
          "method": "DELETE",
          "uriTemplate": "/repos/{owner}/{repo}"
        },
        {
          "method": "GET",
          "uriTemplate": "/repos/{owner}/{repo}/actions/artifacts"
        },
        {
          "method": "GET",
          "uriTemplate": "/repos/{owner}/{repo}/actions/artifacts/{artifact_id}"
        },
        {
          "method": "DELETE",
          "uriTemplate": "/repos/{owner}/{repo}/actions/artifacts/{artifact_id}"
        }
      ]
    }
  }
}
```

## File structure
```bash
 └─.kiota
    └─plugins
       └─GraphPlugin.yaml
       └─GitHub.json
 └─generated
    └─plugins
       └─graphplugin
          └─graphplugin-typeb.json
          └─graphplugin-apimanifest.json
       └─github
          └─github-typea.json
          └─github-apimanifest.json
 └─kiota-config.json
 └─apimanifest.json
```

[def]: https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html#section-2.5-3