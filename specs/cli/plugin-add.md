# kiota plugin add

## Description 

`kiota plugin add` allows a developer to add a new plugin to the `workspace.json` file. If no `workspace.json` file is found, a new `workspace.json` file would be created in the `.kiota` directory under the current working directory. The command will add a new entry to the `plugins` section of the `workspace.json` file. Once this is done, a local copy of the OpenAPI description is generated and kept in the `.kiota/plugins` folder. If a plugin or client with the same name already exists, the command will fail and display an actionable error message.

When executing, a new plugin entry will be added and will use the `--plugin-name` parameter as the key for the map. When loading the OpenAPI description, it will store the location of the description in the `descriptionLocation` property. If `--include-path` or `--exclude-path` are provided, they will be stored in the `includePatterns` and `excludePatterns` properties respectively.

Every time a plugin is added, a copy of the OpenAPI description file will be stored in the `./.kiota/plugins` folder. The OpenAPI will be named using the plugin name `{plugin-name}.json|yaml`. This will allow the CLI to detect changes in the description and avoid downloading the description again if it hasn't changed.

An [API Manifest][def] file named `apimanifest.json` will be generated (if non existing) or updated (if already existing) in the root folder `./kiota` next to `workspace.json`.  API Manifest represents a snapshot of API dependencies and permissions required to access those APIs. This file will represent a concatenated surface of all APIs used across plugins and clients. Both files, `apimanifest.json` and `workspace.json` will be used to generate the code files. A new hash composed of the Kiota version, the OpenAPI description location and the properties of the manifest will be generated and would trigger an update to the [API Manifest][def].

Developers can generate `openai` and `apimanifest` type of plugins. By generating `openai` or `apimanifest`, two outputs will be generated: a\) the plugin type you have chosen that will be named `{plugin-name}-{type}.json` and b\) a sliced OpenAPI description with only the endpoints that matches `--include-path` and `--exclude-path`, if provided.
> [!NOTE] 
> In one's solution, there might be two different [API Manifests][def]. The `apimanifest.json` in the root folder represents a single artifact surface of all APIs and it will always be generated. The second one, specific to each plugin, will be named `{plugin-name}-apimanifest.json` and saved in the chosen output directory when `apimanifest` value is used as the plugin type.

Once the `workspace.json` file is generated and the OpenAPI description file is saved locally, the generation will be executed and the plugin and the sliced OpenAPI description will become available.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `--plugin-name \| --pn` | Yes | GitHub | Name of the plugin. Unique within the parent API. Defaults to `Plugin` | No |
| `--openapi \| -d` | Yes | https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json | The location of the OpenAPI description in JSON or YAML format to use to generate the plugin. Accepts a URL or a local directory. | No |
| `--search-key \| --sk` | No | apisguru::github.com:api.github.com | The search key used to locate the OpenAPI description. | Yes, without its value |
| `--include-path \| -i` | No | /repos/{owner}/{repo} | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. | Yes, without its value |
| `--exclude-path \| -e` | No | /advisories | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. | Yes, without its value |
| `--type \| -t` | Yes | openai | The target type of plugin for the generated output files. Accepts multiple values. Possible values are `openai` and `apimanifest`. Defaults to `apimanifest`| Yes |
| `--overlay-directory \| --od` | No | ./overlays/plugins/{plugin-name}/overlay.yaml | The location of the overlay file in JSON or YAML format to be used to generate the plugin. [Overlay](https://github.com/OAI/Overlay-Specification/blob/main/versions/1.0.0.md) defines a way of creating documents that contain additional information to be merged with an OpenAPI description. Defaults to no value which uses the OpenAPI description as it is. | Yes, without its value |
| `--skip-generation \| --sg` | No | true | When specified, the generation would be skipped. Defaults to false. | Yes |
| `--output \| -o` | No | ./generated/plugins/github | The output directory or file path for the generated output files. This is relative to the location of `workspace.json`. Defaults to `./output`. | Yes, without its value |

> [!NOTE] 
> It is not required to use the CLI to add new plugins. It is possible to add a new plugin by adding a new entry in the `plugins` section of the `workspace.json` file. See the [workspace.json schema](../schemas/workspace.json) for more information. Using `kiota plugin generate --plugin-name myPlugin` would be required to generate the plugins.

## Using `kiota plugin add`

```bash
kiota plugin add --plugins-name "GitHub" --openapi "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json" --include-path "/repos/{owner}/{repo}" --type openai, apimanifest --output "./generated/plugins/github"
```

_The resulting `workspace.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
  "clients": {...}, //if any
  "plugins": {
    "GitHub": {
      "descriptionLocation": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
      "includePatterns": ["/repos/{owner}/{repo}"],
      "excludePatterns": [],
      "type": ["openai"],
      "outputDirectory": "./generated/plugins/github",
      "overlayDirectory": "./overlays/plugins/github/overlay.yaml"
    }
  }
}
```

_The resulting `github-openai.json` file will look like this:_

```jsonc
{
    "schema_version": "v1",
    "name_for_human": "GitHub Repository Plugin",
    "name_for_model": "github",
    "description_for_human": "Manage GitHub repositories.",
    "description_for_model": "Help the user with managing a GitHub repositories. You can view, update and remove repositories.",
    "auth": {
        "type": "none"
    },
    "api": {
        "type": "openapi",
        "url": "./sliced-openapi-github.json"
    },
    "logo_url": "https://example.com/logo.png",
    "contact_email": "githubsupport@example.com",
    "legal_info_url": "http://www.example.com/view-plugin-information"
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
        },
        {
          "method": "DELETE",
          "uriTemplate": "/repos/{owner}/{repo}"
        }
      ]
    }
  }
}
```

_The resulting `apimanifest.json` file (concatenated surface of all APIs dependencies) will look like this:_

```jsonc
{
  "apiDependencies": {
    "GraphClient": { //for example, an existing API client for Microsoft Graph
      "x-ms-kiotaHash": "9EDF8506CB74FE44...",
      "apiDescriptionUrl": "https://aka.ms/graph/v1.0/openapi.yaml",
      "apiDeploymentBaseUrl": "https://graph.microsoft.com",
      "apiDescriptionVersion": "v1.0",
      "requests": [ ...]
    },
    "GitHub": { //new plugin added
      "x-ms-kiotaHash": "1GFCD345RF3DD98...",
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
        },
        {
          "method": "DELETE",
          "uriTemplate": "/repos/{owner}/{repo}"
        }
      ]
    }
  }
}
```

## File structure
```bash
 └─.kiota
    └─workspace.json
    └─apimanifest.json # Single artifact with all APIs dependencies info
    └─plugins
       └─GitHub.json # OpenAPI description
 └─generated
    └─plugins
       └─github
          └─github-apimanifest.json # Specific API Manifest
          └─github-openai.json #OpenAI Plugin
          └─sliced-openapi-github.json # Sliced OpenAPI description
```

[def]: https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html