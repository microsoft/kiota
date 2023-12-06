# Kiota Config

Kiota generates client code for an API and stores parameters in a kiota.lock file. A project can contain multiple API clients, but they are independently managed. Kiota has no awareness that an app has a dependency on multiple APIs, even though that is a core use case.

## Status

| Date | Version | Author | Status |
| -- | -- | -- | -- |
| November 30th, 2023 | v0.3 | Sébastien Levert | Final Draft |
| November 22nd, 2023 | v0.2 | Sébastien Levert | Draft |
| September 24th, 2023 | v0.1 | Darrel Miller | Draft |

## Current Challenges

- Client code generation is not reproducible if API description changes
- Kiota doesn’t have a good solution for APIs that use multiple security schemes.
- Kiota doesn’t provide any support for generating auth providers with the required permissions, partially because currently we generate one client for APIs that use different schemes. How would we know which auth provider to generate.
- Kiota doesn’t have a good story for acquiring a client identifier. e.g. apikey or OAuth2 ClientId. This could be possible if the OpenIDConnect URL pointed to a dynamic registration endpoint.
- If an application has multiple kiota clients, there is currently no way perform operations that correspond to all of the clients.

We have previously described Kiota's approach to managing API dependencies as consistent with the way people manage packages in a project. However, currently our tooling doesn't behave that way. We treat each dependency independently.

## Proposal

We should introduce a new Kiota.config file that holds the input parameters required to generate the API Client code. Currently kiota.lock is used to capture what the parameters were at the time of generation and can be used to regenerate based on the parameters in the file. This creates a mixture of purposes for the file.

We did consider creating one kiota.config file as as a peer of the language project file, however, for someone who wants to generate multiple clients for an API in different languages, this would be a bit annoying. An alternative would be to allow the kiota.config file to move further up the folder structure and support generation in multiple languages from a single file. This is more consistent with what [TypeSpec](https://aka.ms/typespec) are doing and would be helpful for generating CLI and docs as well as a library.

Here is an example of what the kiota.config file could look like.

```json
{
  "name": "My application",
  "apis": {
    "Graph": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://.../openapi.yaml",
      "includePatterns": ["/me/chats#GET", "/me#GET"],
      "excludePatterns": [],
      "clients": [
        {
          "language": "csharp",
          "outputPath": "./generated/graph/csharp",
          "clientClassName": "GraphClient",
          "clientNamespaceName": "Contoso.GraphApp",
          "features": {
            "authentication": {
              "authenticationProvider": "Microsoft.Kiota.Authentication.AzureAuthProvider",
              "authenticationParameters": {
                "clientId": "guid"
              }
            },
            "usesBackingStore": true,
            "includeAdditionalData": true
          }
        }
      ]
    },
    "BusinessCentral": {
      "descriptionHash": "810CF81EFDB5D8E065...",
      "descriptionLocation": "https://.../bcoas1.0.yaml",
      "includePatterns": ["/companies#GET"],
      "excludePatterns": [],
      "outputs": [
        {
          "language": "csharp",
          "outputPath": "./generated/business-central"
        },
        {
          "language": "python",
          "outputPath": "./generated/python/business-central"
        },
        {
          "language": "csharp",
          "outputPath": "./generated/business-central-app",
          "features": {
            "authentication": {
              "authenticationProvider": "Microsoft.Kiota.Authentication.AzureAuthProvider",
              "authenticationParameters": {
                "clientId": "guid"
              }
            }
          }
        }
      ]
    }
  }
}
```

Note that in this example we added suggestions for new parameters related to authentication. If we are to improve the generation experience so that we read the security schemes information from the OpenAPI, then we will need to have some place to configure what providers we will use for those schemes.

The [API Manifest](https://www.ietf.org/archive/id/draft-miller-api-manifest-01.html) file can be used as a replacement for the kiota.lock file as a place to capture a snapshot of what information was used to perform code generation and what APIs that gives the application access to.

## Commands

* [kiota config init](../cli/init.md)
* [kiota client add](../cli/client-add.md)
* [kiota client edit](../cli/client-edit.md)
* [kiota client generate](../cli/client-remove.md)
* [kiota client remove](../cli/client-remove.md)

## End-to-end experience

#### Using kiota generate with parameters inferred from the kiota.config file

```bash
kiota generate
```

#### Using kiota generate with parameters inferred from the kiota.config file for a single API

```bash
kiota generate --api-name "graph" --client-name "graphDelegated"
```

#### Using kiota generate with parameters inferred when there are no kiota.config file

```bash
kiota generate --openapi "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml" --include-path "/me/chats#GET" --include-path "/me#GET" --language csharp --class-name "GraphClient" --namespace-name "Contoso.GraphApp" --backing-store --exclude-backward-compatible --serializer "Contoso.Json.CustomSerializer" --deserializer "Contoso.Json.CustomDeserializer" -structured-mime-types "application/json" --output "./generated/graph/csharp"
```

```javascript
// This file gets generated and then `kiota generate` is executed based on these parameters
{
  "name": "Contoso.GraphApp", // Inferred from the provided --namespace-name or its default value
  "apis": {
    "https://graph.microsoft.com/v1.0": { // Inferred from the first server entry in the OpenAPI description
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://.../openapi.yaml",
      "includePatterns": ["/me/chats#GET", "/me#GET"],
      "excludePatterns": [],
      "clients": {
        "GraphClient": { // Inferred from the provided --class-name or its default value 
          "language": "csharp",
          "outputPath": "./generated/graph/csharp",
          "clientClassName": "GraphClient",
          "clientNamespaceName": "Contoso.GraphApp",
          "features": {
            // Adding for future visibility, but not required for now
            /*"authentication": {
              "authenticationProvider": "Microsoft.Kiota.Authentication.AzureAuthProvider",
              "authenticationParameters": {
                "clientId": "guid"
              },
            },*/
            "serializers": [
              "Contoso.Json.CustomSerializer"
            ],
            "deserializers": [
              "Contoso.Json.CustomDeserializer"
            ],
            "structuredMimeTypes": [
              "application/json"
            ],
            "usesBackingStore": true,
            "includeAdditionalData": true
          }
        }
      }
    }
  }
}
```

## End-to-end scenarios using the CLI

### Get started to generate an API

```bash
kiota init --app-name "My Application"
kiota api add --api-name "My API" --openapi "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml"
kiota client add --api-name "My API" --clientName "graphDelegated" --language csharp --outputPath ./csharpClient
kiota generate
```

### Add a second language to generate an API

```bash
kiota client add --api-name "My API" --clientName "graphPython" --language python --outputPath ./pythonClient
kiota generate --api-name "My API" --client-name "graphPython"
```

### Remove a language and delete the generated code

```bash
kiota client delete --api-name "My API" --client=name "graphPython" --clean-output
```

### Remove an API

```bash
kiota api delete --name "My Api" --clean-output
```
