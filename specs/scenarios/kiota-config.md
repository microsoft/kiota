# Kiota Config

Kiota generates client code for an API and stores parameters in a kiota-lock.json file. A project can contain multiple API clients, but they are independently managed. Kiota has no awareness that an app has a dependency on multiple APIs, even though that is a core use case.

## Current Challenges

- Client code generation is not reproducible if API description changes
- Kiota doesn't have an obvious solution for APIs that use multiple security schemes.
- Kiota doesn't have an obvious solution for projects utilizing multiple APIs.
- Kiota is currently specialized in client code generation and [API Manifest](https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html#section-2.5-3) and it doesn't meet the market expansion for AI scenarios.

We have previously described Kiota's approach to managing API dependencies as consistent with the way people manage packages in a project. However, currently our tooling doesn't behave that way. We treat each dependency independently.

## Proposal

We should introduce a new Kiota.config file that holds the input parameters required to generate either the API Client code or AI plugin manifests. Currently kiota-lock.json is used to capture what the parameters were at the time of generation and can be used to regenerate based on the parameters in the file. This creates a mixture of purposes for the file.

We did consider creating one kiota-config.json file as a peer of the language project file, however, for someone who wants to generate multiple clients in different languages and plugin manifests for an API, this would be a bit annoying. An alternative would be to allow the kiota-config.json file to move further up the folder structure and support generation in multiple languages from a single file. This is more consistent with what [TypeSpec](https://aka.ms/typespec) are doing and would be helpful for generating CLI and docs as well as a library.

Here is an example of what the kiota-config.json file could look like.

```jsonc
{
  "version": "1.0.0",
  "clients": {
    "GraphClient": {
      "descriptionLocation": "https://aka.ms/graph/v1.0/openapi.yaml",
      "includePatterns": ["**/users/**"],
      "excludePatterns": [],
      "language": "csharp",
      "outputPath": "./generated/graph",
      "clientClassName": "GraphClient",
      "clientNamespaceName": "Contoso.GraphApp",
      "structuredMediaTypes": [
        "application/json"
      ],
      "usesBackingStore": true,
      "includeAdditionalData": true
    },
    "businessCentral": {
      "descriptionLocation": "https://.../bcoas1.0.yaml",
      "includePatterns": ["/companies#GET"],
      "excludePatterns": [],
      "language": "csharp",
      "outputPath": "./generated/businessCentral"
    },
    "plugins": {
      "GitHub": {
        "descriptionLocation": "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json",
        "includePatterns": ["/repos/**"],
        "excludePatterns": [],
        "type": "typea",
        "outputDirectory": "./generated/plugins/github",
        "overlayDirectory": "./overlays/plugins/github/overlay.yaml"
      }
    }
  }
}
```

Note that in this example we added suggestions for new parameters related to authentication. If we are to improve the generation experience so that we read the security schemes information from the OpenAPI, then we will need to have some place to configure what providers we will use for those schemes.

The [API Manifest](https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html) file can be used as a replacement for the kiota-lock.json file as a place to capture a snapshot of what information was used to perform code generation and what APIs that gives the application access to.

## Commands

* [kiota config init](../cli/config-init.md)
* [kiota client add](../cli/client-add.md)
* [kiota client edit](../cli/client-edit.md)
* [kiota client generate](../cli/client-generate.md)
* [kiota client remove](../cli/client-remove.md)
* [kiota manifest add](../cli/manifest-add.md)
* [kiota manifest edit](../cli/manifest-edit.md)
* [kiota manifest generate](../cli/manifest-generate.md)
* [kiota manifest remove](../cli/manifest-remove.md)

## End-to-end experience

### APIs clients

- #### Migrate a project that uses Kiota v1.x for API client

```bash
kiota config migrate
```

- #### Get started to generate an API client

```bash
kiota client init
kiota client add --client-name "GraphClient" --openapi "https://aka.ms/graph/v1.0/openapi.yaml" --language csharp --output "./csharpClient"
```

- #### Add a second API client

```bash
kiota client add  --clientName "graphPython" --openapi "https://aka.ms/graph/v1.0/openapi.yaml" --language python --outputPath ./pythonClient
```

- #### Edit an API client

```bash
kiota client edit --client-name "GraphClient" --exclude-path "/users/$count"
```

- #### Remove a language and delete the generated code

```bash
kiota client delete --client=name "graphPython" --clean-output
```

- #### Generate code for all API clients

```bash
kiota client generate
```

### AI Plugin Manifests

- #### Add a plugin manifest

```bash
kiota manifest add --plugin-name "GitHub" --openapi "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.json" --include-path "/repos/**" --type typea --output "./generated/plugins/github"
```

- #### Edit an AI plugin manifest

```bash
kiota manifest edit --plugin-name "GitHub" --exclude-path "/repos/{owner}/{repo}/actions/cache/usage"
```

- #### Remove an AI plugin manifest

```bash
kiota manifest remove --plugin-name "GitHub" --clean-output
```

- #### Generate all AI plugin manifests

```bash
kiota manifest generate
```
