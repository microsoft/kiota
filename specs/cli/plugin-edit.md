# kiota plugin edit

## Description

`kiota plugin edit` allows a developer to edit an existing plugin in the `workspace.json` file. If either the `workspace.json` file or if the `--plugin-name` plugin can't be found within the `workspace.json` file, the command should error out and let the developer know.

When executing, the plugin entry defined by the `--plugin-name` parameter will be modified. All parameters should be supported and the only required one is `--plugin-name`. All others are optional as they would only modify the configuration of the plugin. If the OpenAPI document location changed or any properties of the plugin entry in `workspace.json`, a new hash composed of the Kiota version, the OpenAPI document location and the properties of the manifest will be generated and and would trigger an update to the [API Manifest][def] located in the root folder (the).
> [!NOTE]
> > In one's solution, there might be two different [API Manifests][def]. The `apimanifest.json` in the `./kiota` folder represents a single artifact surface of all APIs and it will always be generated. The second one, specific to each plugin when providing `--type apimanifest`, will be named `{plugin-name}-apimanifest.json` and saved in the chosen output directory.

Once the `workspace.json` file and the API Manifest are updated, the code generation will be executed based on the newly updated API client configuration.

## Parameters

| Parameters | Required | Example | Description | Telemetry |
| -- | -- | -- | -- | -- |
| `--plugin-name \| --pn` | Yes | GitHub | Name of the plugin. Unique within the parent API. Defaults to `Plugin` | No |
| `--openapi \| -d` | Yes | <https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json> | The location of the OpenAPI document in JSON or YAML format to use to generate the plugin. Accepts a URL or a local directory. | Yes, without its value |
| `--include-path \| -i` | No | /repos/{owner}/{repo} | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. | Yes, without its value |
| `--exclude-path \| -e` | No | /repos/{owner}/{repo}#DELETE | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. | Yes, without its value |
| `--type \| -t` | Yes | openai | The target type of plugin for the generated output files. Accepts multiple values. Possible values are `apiplugin`, `openai` and `apimanifest`.| Yes |
| `--authentication-type \| --at` | No | oauth2 | The authentication type that will be used to connect to the API. Accepts a single value corresponding to a supported OpenAPI security scheme. Possible values are `apikey`, `http`, `oauth2` and `openidconnect`.| Yes |
| `--authentication-ref-id \| --refid` | No | xxxxxxxx | The authentication reference id that will be used to connect to the API. Accepts a single string value.| Yes, without its value |
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
      "authType": "OAuthPluginVault",
      "authReferenceId": "somerefid",
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

_The resulting API Plugin named `github-apiplugin.json` will look like this:_

```jsonc
{
  "$schema": "https://aka.ms/json-schemas/copilot-extensions/v2.3/plugin.schema.json",
  "schema_version": "v2.3",
  "name_for_human": "GitHub v3 REST API",
  "description_for_human": "GitHub\u0026apos;s v3 REST API.",
  "description_for_model": "GitHub\u0026apos;s v3 REST API.",
  "logo_url": "https://api.apis.guru/v2/cache/logo/https_twitter.com_github_profile_image.jpeg",
  "contact_email": "publisher-email@example.com",
  "namespace": "GitHubReposOwner",
  "capabilities": {
    "localization": {}
  },
  "functions": [
    {
      "name": "repos_get",
      "description": "Get a repository"
    },
    {
      "name": "repos_update",
      "description": "Update a repository"
    }
  ],
  "runtimes": [
    {
      "type": "OpenApi",
      "auth": {
        "type": "OAuthPluginVault",
        "reference_id": "somerefid"
      },
      "spec": {
        "url": "githubreposowner-openapi.yml"
      },
      "run_for_functions": [
        "repos_get",
        "repos_update"
      ]
    }
  ]
}
```

_The resulting API Manifest named `github-apimanifest.json` will look like this:_

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

_The resulting API Manifest named `apimanifest.json` in the `./kiota` folder (concatenated surface of all APIs dependencies across clients and plugins) will look like this:_

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
        └─openapi.json # OpenAPI document
        └─overlay.json # Overlay to be applied on top of OpenAPI document
 └─generated
    └─plugins
      └─github
          └─github-apimanifest.json # Specific API Manifest
          └─github-apiplugin.json #API Plugin
          └─github-openapi.json # Sliced and augmented OpenAPI document
```

[def]: https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html
