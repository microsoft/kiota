# Kiota Config

Kiota generates client code for an API and stores parameters in a kiota.lock file. A project can contain multiple API clients, but they are independently managed. Kiota has no awareness that an app has a dependency on multiple APIs, even though that is a core use case.

## Status

| Date | Version | Author | Status |
| -- | -- | -- | -- |
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
  "version": "2.0",
  "apis": {
    "Graph": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://.../openapi.yaml",
      "includePatterns": ["/me/chats#GET", "/me#GET"],
      "excludePatterns": [],
      "outputs": [
        {
          "language": "csharp",
          "outputPath": "./generated/graph",
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

## Tooling commands to manage Kiota.config

| Command                | Example | Description           |
| ------------------- | ------- | ---------------- |
| init | kiota init --name <appName> | Creates a kiota.config file |
| add api | kiota add api --name <apiname> --openapi <urlToApiDescription> | Adds an entry for an API with passed parameters and default values |
| add output | kiota add output --api-name <apiname> --lang python --outputPath ./pythonClient | Adds information about a new output artifact that should be generated |
| generate | kiota generate | Outputs kiota.apimanifest and source for each of the output objects |

In the past we have had both a generate and an update comment. This is because if it was the first time running, generate would set defaults for all the properties that were not set on the command line. However, now that we have add output, it can be used to set the defaults and generate can just read from the kiota.config file.

## Commands

### kiota init

`kiota init` creates a new kiota.config file with the passed parameters. If the file already exists, it should error out and report it to the user. The initialization process has a single required parameter, the name of the application.

> [!NOTE] 
> If a project only needs a single API, using `kiota init` is not required as generating code using the `kiota generate` command should generate a `kiota.config` file with values coming from the `kiota generate` command. See [kiota generate](#kiota-generate) for more information.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--app-name \| -n` | Yes | My application | Name of the application |

#### Using `kiota init`

```bash
kiota init --app-name "My application"
```

```json
// Creates the following kiota.config file
{
  "name": "My application",
  "version": "1.0"
}
```

### kiota add api

`kiota add api` allows a developer to add a new API to the kiota.config file. The command will add a new entry to the apis section of the kiota.config file. The command has two required parameters, the name of the API (key of the api map) and the location of the OpenAPI description. The command also has two optional parameters, the include and exclude patterns. If provided, these will be used to filter the paths that are included in the generation process. If not provided, all paths will be assumed.

When executing, a new API entry will be added and will use the `--api-name` parameter as the key for the map. When loading the OpenAPI description, it will generate a hash of the description to enable change detection of the description and save it as part of the `descriptionHash` property. It will also store the location of the description in the `descriptionLocation` property. If `--include-path` or `--exclude-path` are provided, they will be stored in the `includePatterns` and `excludePatterns` properties respectively.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--api-name \| -n` | Yes | graph | Name of the API |
| `--open-api \| -d` | Yes | https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml | The location of the OpenAPI description in JSON or YAML format to use to generate the SDK. Accepts a URL or a local path. |
| `--include-path \| -i` | No | /me/chats#GET | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. |
| `--exclude-path \| -e` | No | \*\*/users/\*\* | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. |

#### Using `kiota add api`

```bash
kiota add api --api-name "graph" --openapi "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml" --include-path "/me/chats#GET" --include-path "/me#GET"
```

```json
// Adds the following to the kiota.config file
"graph": {
  "descriptionHash": "9EDF8506CB74FE44...",
  "descriptionLocation": "https://.../openapi.yaml",
  "includePatterns": ["/me/chats#GET", "/me#GET"],
  "excludePatterns": []
}
```

### kiota generate

In scenarios where a developer only needs a single API or doesn't want to go through the ceremony of executing `kiota init`, it's possible to use `kiota generate` as it will create a `kiota.config` file with the values coming from the command parameters.

#### Using `kiota generate`

```bash
```

```json
```

## Scenarios using the command line tool

### Get started to generate an API

```bash
kiota init --name Myapp
kiota add api --name MyApi --openapi <https://example.org/api/openapi.json>   // Can we add using -k ?
kiota add output --name MyApi --lang csharp --outputPath ./csharpClient
kiota generate
```

### Add a second language to generate an API

```bash
kiota add output --name MyApi --lang python --outputPath ./pythonClient
kiota generate --name MyApi --lang python  // Generate just the Python client for MyApi
```

### Remove a language

```bash
kiota remove output --name MyApi --lang python
```

### Remove an API

```bash
kiota remove api --name MyApi
```

## JSON Schema for Kiota.Config

```json
{
  "$schema": "<http://json-schema.org/draft-07/schema#>",
  "type": "object",
  "properties": {
    "name": {
      "type": "string"
    },
    "apis": {
      "type": "object",
      "patternProperties": {
        ".*": {
          "type": "object",
          "properties": {
            "descriptionLocation": {
              "type": "string"
            },
            "descriptionHash": {
              "type": "string"
            }
          },
          "descriptionHash": {
            "type": "string"
          },
          "descriptionLocation": {
            "type": "string"
          },
          "includePatterns": {
            "type": "array",
            "items": {
              "type": "string"
            }
          },
          "excludePatterns": {
            "type": "array",
            "items": {
              "type": "string"
            }
          },
          "baseUrl": {
            "type": "string"
          },
          "output": {
            "type": "array",
            "items": {
              "type": "object",
              "properties": {
                "language": {
                  "type": "string"
                },
                "outputPath": {
                  "type": "string"
                },
                "clientClassName": {
                  "type": "string"
                },
                "clientNamespaceName": {
                  "type": "string"
                },
                "features": {
                  "type": "object",
                  "properties": {
                    "authenticationProvider": {
                      "type": "string"
                    },
                    "authenticationParameters": {
                      "type": "object"
                    }
                  },
                  "structuredMediaTypes": {
                    "type": "object",
                    "patternProperties": {
                      ".*": {
                        "type": "object",
                        "properties": {
                          "serializer": {
                            "type": "string"
                          },
                          "deserializer": {
                            "type": "string"
                          }
                        }
                      }
                    }
                  },
                  "usesBackingStore": {
                    "type": "boolean"
                  },
                  "includeAdditionalData": {
                    "type": "boolean"
                  }
                }
              }
            }
          }
        },
        "disabledValidationRules": {
          "type": "array",
          "items": {
            "type": "string"
          }
        }
      }
    }
  }
}
```
