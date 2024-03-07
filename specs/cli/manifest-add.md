# kiota manifest add

## Description 

`kiota manifest add` allows a developer to add a new manifest to the `kiota-config.json` file. If no `kiota-config.json` file is found, a new `kiota-config.json` file would be created in the current working directory. The command will add a new entry to the `manifests` section of the `kiota-config.json` file. Once this is done, a local copy of the OpenAPI description is generated and kept in the `.kiota/manifests` folder. If a manifest with the same name already exists, the command will fail and display an actionnable error message.

When executing, a new manifest entry will be added and will use the `--manifest-name` parameter as the key for the map. When loading the OpenAPI description, it will store the location of the description in the `descriptionLocation` property. If `--include-path` or `--exclude-path` are provided, they will be stored in the `includePatterns` and `excludePatterns` properties respectively.

Every time a manifest is added, a copy of the OpenAPI description file will be stored in the `./.kiota/manifests` folder. The OpenAPI will be named using the manifest name `{manifest-name}.json|yaml`. This will allow the CLI to detect changes in the description and avoid downloading the description again if it hasn't changed.

An [API Manifest][def] file named `apimanifest.json` will be generated (if non existing) or updated (if already existing) in the root folder next to `kiota-config.json`. This file will represent a concatenated surface of all APIs used across manifests and clients. Both files, `apimanifest.json` and `kiota-config.json` will be used to generate the code files. A new hash composed of the Kiota version, the OpenAPI description location and the properties of the manifest will be generated and would trigger an update to the [API Manifest][def].

Developers can generate `openai` and `apimanifest` type of manifests. By generating `openai` or `apimanifest`, two outputs will be generated: a\) the manifest type you have choosen that will be named `{manifest-name}-{type}.json` and b\) a sliced OpenAPI description with only the endpoints that matches `--include-path` and `--exclude-path`, if provided.
> [!NOTE] 
> In one's solution, there might be two different [API Manifests][def]. The `apimanifest.json` in the root folder represents a single artifact surface of all APIs and it will always be generated. The second one, specific to each manifest, will be named `{manifest-name}-apimanifest.json` and saved in the choosen output directory when `apimanifest` value is used as the manifest type.

Once the `kiota-config.json` file is generated and the OpenAPI description file is saved locally, the generation will be executed and the manifest and the sliced OpenAPI description will become available.

## Parameters

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--manifest-name \| --pn` | Yes | GitHub | Name of the manifest. Unique within the parent API. Defaults to `Manifest` |
| `--openapi \| -d` | Yes | https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json | The location of the OpenAPI description in JSON or YAML format to use to generate the manifest. Accepts a URL or a local directory. |
| `--include-path \| -i` | No | /repos/{owner}/{repo} | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. |
| `--exclude-path \| -e` | No | /advisories | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. |
| `--type \| -t` | Yes | openai | The target type of manifest for the generated output files. Accepts multiple values. Possible values are `openai` and `apimanifest`. Defaults to `apimanifest`|
| `--overlayDirectory \| --od` | No | ./overlay/manifest/{manifest-name}/overlay.yaml | The location of the overlay file in JSON or YAML format to be used to generate the manifest. [Overlay](https://github.com/OAI/Overlay-Specification/blob/main/versions/1.0.0.md) defines a way of creating documents that contain additional information to be merged with an OpenAPI description. Defaults to no value which uses the OpenAPI description as it is. |
| `--skip-generation \| --sg` | No | true | When specified, the generation would be skipped. Defaults to false. |
| `--output \| -o` | No | ./generated/manifest/github | The output directory or file path for the generated output files. This is relative to the location of `kiota-config.json`. Defaults to `./output`. |

> [!NOTE] 
> It is not required to use the CLI to add new manifests. It is possible to add a new manifest by adding a new entry in the `manifests` section of the `kiota-config.json` file. See the [kiota-config.json schema](../schemas/kiota-config.json) for more information. Using `kiota manifest generate --manifest-name myManifest` would be required to generate the manifests.

## Using `kiota manifest add`

```bash
kiota manifest add --manifest-name "GitHub" --openapi "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json" --include-path "/repos/{owner}/{repo}" --type openai, apimanifest --output "./generated/manifests/github"
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
      "excludePatterns": [],
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
    "description_for_model": "Help the user with managing a GitHub repositories. You can view, update and remove repositories.",
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
    "GitHub": { //new manifest added
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
    └─manifests
       └─GitHub.json # OpenAPI description
 └─generated
    └─manifests
       └─github
          └─github-apimanifest.json # Specific apimanifest
          └─github-openai.json #OpenAI manifest
          └─sliced-openapi-github.json # Sliced OpenAPI description
 └─kiota-config.json
 └─apimanifest.json # Single artifact with all APIs info
```

[def]: https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html#section-2.5-3