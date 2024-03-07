# kiota manifest edit

## Description 

`kiota manifest update` allows a developer to edit an existing manifest in the `kiota-config.json` file. If either the `kiota-config.json` file or if the `--manifest-name` manifest can't be found within the `kiota-config.json` file, the command should error out and let the developer know.

When executing, the manifest entry defined by the `--manifest-name` parameter will be modified. All parameters should be supported and the only required one is `--manifest-name`. All others are optional as they would only modify the configuration of the manifest. If the OpenAPI description location changed or any properties of the manifest entry in `kiota-config.json`, a new hash composed of the Kiota version, the OpenAPI description location and the properties of the manifest will be generated and and would trigger an update to the [API Manifest][def] located in the root folder (the).
> [!NOTE] 
> > In one's solution, there might be two different [API Manifests][def]. The `apimanifest.json` in the root folder represents a single artifact surface of all APIs and it will always be generated. The second one, specific to each manifest, will be named `{manifest-name}-apimanifest.json` and saved in the choosen output directory when `apimanifest` value is used as the manifest type.

Once the `kiota-config.json` file and the API Manifest are updated, the code generation will be executed based on the newly updated API client configuration.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--manifest-name \| --pn` | Yes | GitHub | Name of the manifest. Unique within the parent API. Defaults to `Manifest` |
| `--openapi \| -d` | Yes | https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json | The location of the OpenAPI description in JSON or YAML format to use to generate the manifest. Accepts a URL or a local directory. |
| `--include-path \| -i` | No | /repos/{owner}/{repo} | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. |
| `--exclude-path \| -e` | No | /repos/{owner}/{repo}#DELETE | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. |
| `--type \| -t` | Yes | openai | The target type of manifest for the generated output files. Accepts multiple values. Possible values are `openai` and `apimanifest`. Defaults to `apimanifest`|
| `--overlayDirectory \| --od` | No | ./overlay/manifests/{manifest-name}/overlay.yaml | The location of the overlay file in JSON or YAML format to be used to generate the manifest. [Overlay](https://github.com/OAI/Overlay-Specification/blob/main/versions/1.0.0.md) defines a way of creating documents that contain additional information to be merged with an OpenAPI description. Defaults to no value which uses the OpenAPI description as it is. |
| `--skip-generation \| --sg` | No | true | When specified, the generation would be skipped. Defaults to false. |
| `--output \| -o` | No | ./generated/manifests/github | The output directory or file path for the generated output files. This is relative to the location of `kiota-config.json`. Defaults to `./output`. |

> [!NOTE] 
> It is not required to use the CLI to edit manifests. It is possible to edit a manifest by modifying its entry in the `manifests` section of the `kiota-config.json` file. See the [kiota-config.json schema](../schemas/kiota-config.json) for more information.

## Using `kiota manifest edit`

```bash
kiota manifest edit --manifest-name "GitHub" --exclude-path "/repos/{owner}/{repo}#DELETE"
```

_The resulting `kiota-config.json` file will look like this:_

```jsonc
{
  "version": "1.0.0",
  "clients": {...}, //if any
  "manifests": {
    "GitHub": {
      "descriptionLocation": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
      "includePatterns": ["/repos/{owner}/{repo}"],
      "excludePatterns": ["/repos/{owner}/{repo}#DELETE"],
      "type": "openai",
      "outputDirectory": "./generated/manifests/github",
      "overlayDirectory": "./overlays/manifests/github/overlay.yaml"
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
        "url": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json"
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
    └─manifests
       └─GitHub.json # OpenAPI description
 └─generated
    └─manifests
       └─github
          └─github-apimanifest.json # Specific apimanifest
          └─github-openai.json #OpenAI manifest
          └─sliced-openapi-github.json # Sliced OpenAPI description
 └─kiota-config.json
 └─apimanifest.json
```

[def]: https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html#section-2.5-3