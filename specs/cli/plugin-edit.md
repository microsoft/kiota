# kiota plugin edit

## Description 

`kiota plugin update` allows a developer to edit an existing plugin in the `workspace.json` file. If either the `workspace.json` file or if the `--plugin-name` plugin can't be found within the `workspace.json` file, the command should error out and let the developer know.

When executing, the plugin entry defined by the `--plugin-name` parameter will be modified. All parameters should be supported and the only required one is `--plugin-name`. All others are optional as they would only modify the configuration of the plugin. If the OpenAPI document location changed or any properties of the plugin entry in `workspace.json`, a new hash composed of the Kiota version, the OpenAPI document location and the properties of the manifest will be generated and and would trigger an update to the [API Manifest][def] located in the root folder (the).
> [!NOTE] 
> > In one's solution, there might be two different [API Manifests][def]. The `apimanifest.json` in the `./kiota` folder represents a single artifact surface of all APIs and it will always be generated. The second one, specific to each plugin, will be named `{plugin-name}-apimanifest.json` and saved in the choosen output directory when `apimanifest` value is used as the plugin type.

Once the `workspace.json` file and the API Manifest are updated, the code generation will be executed based on the newly updated API client configuration.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `--plugin-name \| --pn` | Yes | GitHub | Name of the plugin. Unique within the parent API. Defaults to `Plugin` | No |
| `--openapi \| -d` | Yes | https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json | The location of the OpenAPI document in JSON or YAML format to use to generate the plugin. Accepts a URL or a local directory. | Yes, without its value |
| `--include-path \| -i` | No | /repos/{owner}/{repo} | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. | Yes, without its value |
| `--exclude-path \| -e` | No | /repos/{owner}/{repo}#DELETE | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. | Yes, without its value |
| `--type \| -t` | Yes | openai | The target type of plugin for the generated output files. Accepts multiple values. Possible values are `openai` and `apimanifest`. Defaults to `apimanifest`| Yes |
| `--overlay \| --od` | No | ./kiota/documents/{plugin-name}/overlay.yaml | The location of the overlay file in JSON or YAML format to be used to generate the plugin. [Overlay](https://github.com/OAI/Overlay-Specification/blob/main/versions/1.0.0.md) defines a way of creating documents that contain additional information to be merged with an OpenAPI document. Defaults to no value which uses the OpenAPI document as it is. | Yes, without its value |
| `--skip-generation \| --sg` | No | true | When specified, the generation would be skipped. Defaults to false. | Yes |
| `--output \| -o` | No | ./generated/plugins/github | The output directory or file path for the generated output files. This is relative to the current working directory. Defaults to `./output`. | Yes, without its value |

> [!NOTE] 
> It is not required to use the CLI to edit plugins. It is possible to edit a plugin by modifying its entry in the `plugins` section of the `workspace.json` file. See the [workspace.json schema](../schemas/workspace.json) for more information.

## Using `kiota plugin edit`

```bash
kiota plugin edit --plugin-name "GitHub" --exclude-path "/repos/{owner}/{repo}#DELETE"
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
      "excludePatterns": ["/repos/{owner}/{repo}#DELETE"],
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
    "description_for_model": "Help the user with managing a GitHub repositories. You can view and update repositories.",
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
        }
      ]
    }
  }
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
    },
    "GitHub": {
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
    └─apimanifest.json
    └─documents
      └─github    
        └─GitHub.json # OpenAPI document
        └─overlay.json # Overlay to be applied on top of OpenAPI document
 └─generated
    └─plugins
      └─github
          └─github-apimanifest.json # Specific API Manifest
          └─github-openai.json #OpenAI Plugin
          └─sliced-openapi-github.json # Sliced and augmented OpenAPI document
```

[def]: https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html