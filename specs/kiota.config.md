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

### kiota init

`kiota init` creates a new kiota.config file with the provided parameters. If the file already exists, it should error out and report it to the user. The initialization process has a single required parameter, the name of the application.

> [!NOTE] 
> If a project only needs a single API, using `kiota init` is not mandatory as generating code using the `kiota generate` command could generate a `kiota.config` file with values coming from the `kiota generate` command (if no `kiota.config` is present). See [kiota generate](#kiota-generate) for more information.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--app-name \| --an` | Yes | My application | Name of the application |

#### Using `kiota init`

```bash
kiota init --app-name "My application"
```

```jsonc
// Creates the following kiota.config file
{
  "name": "My application"
}
```

### kiota api add

`kiota api add` allows a developer to add a new API to the `kiota.config` file. The command will add a new entry to the `apis` section of the `kiota.config` file. The command has two required parameters, the name of the API (key of the apis map) and the location of the OpenAPI description. The command also has two optional parameters, the include and exclude patterns. If provided, these will be used to filter the paths that are included in the future generation process. If not provided, all paths will be assumed.

When executing, a new API entry will be added and will use the `--api-name` parameter as the key for the map. When loading the OpenAPI description, it will generate a hash of the description to enable change detection of the description and save it as part of the `descriptionHash` property. It will also store the location of the description in the `descriptionLocation` property. If `--include-path` or `--exclude-path` are provided, they will be stored in the `includePatterns` and `excludePatterns` properties respectively.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--api-name \| --api` | Yes | graph | Name of the API |
| `--openapi \| -d` | Yes | https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml | The location of the OpenAPI description in JSON or YAML format to use to generate the SDK. Accepts a URL or a local path. |
| `--include-path \| -i` | No | /me/chats#GET | A glob pattern to include paths from generation. Accepts multiple values. Defaults to no value which includes everything. |
| `--exclude-path \| -e` | No | \*\*/users/\*\* | A glob pattern to exclude paths from generation. Accepts multiple values. Defaults to no value which excludes nothing. |

#### Using `kiota api add`

```bash
kiota api add --api-name "graph" --openapi "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml" --include-path "/me/chats#GET" --include-path "/me#GET"
```

```jsonc
// Adds the following to the kiota.config file
"graph": {
  "descriptionHash": "9EDF8506CB74FE44...",
  "descriptionLocation": "https://.../openapi.yaml",
  "includePatterns": ["/me/chats#GET", "/me#GET"],
  "excludePatterns": []
}
```

The resulting `kiota.config` file will look like this:

```jsonc
{
  "name": "My application",
  "apis": {
    "graph": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://.../openapi.yaml",
      "includePatterns": ["/me/chats#GET", "/me#GET"],
      "excludePatterns": []
    }
  }
}
```

### kiota api delete

`kiota api delete` allows a developer to delete an existing API from the `kiota.config` file. The command will remove the entry from the `apis` section of the `kiota.config` file. The command has one required parameter, the name of the API (key of the apis map). The command also has one optional parameter, the ability to remove generated clients. If provided, kiota will delete the folder specified at the `outputPath` from the client configuration.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--api-name \| --api` | Yes | graph | Name of the API |
| `--clean-output \| --co` | No |  | Cleans the generated clients from the API |

#### Using kiota api delete

```bash
kiota api delete --api-name "graph" --clean-output
```

```jsonc
// Removes the following from the kiota.config file
"graph": {
  "descriptionHash": "9EDF8506CB74FE44...",
  "descriptionLocation": "https://.../openapi.yaml",
  "includePatterns": ["/me/chats#GET", "/me#GET"],
  "excludePatterns": [],
  "clients": {
    // All clients
  }
} 
```

The resulting `kiota.config` file will look like this:

```jsonc
{
  "name": "My application",
  "apis": {}
}
```

### kiota client add

`kiota client add` allows a developer to add a new client for a specified API to the `kiota.config` file. The command will add a new entry to the `clients` section of the `kiota.config` file. The command has two required parameters, the name of the API (key of the apis map) and the location of the OpenAPI description. The command also has two optional parameters, the include and exclude patterns. If provided, these will be used to filter the paths that are included in the future generation process. If not provided, all paths will be assumed. The `kiota client add` command will never automatically invoke `kiota generate`.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--api-name \| --api` | Yes | graph | Name of the API |
| `--client-name \| --cn` | No | graphDelegated | Name of the client. Unique within the parent API. If not provided, defaults to --class-name or its default. |
| `--language \| -l` | Yes | csharp | The target language for the generated code files or for the information. |
| `--class-name \| -c` | No | GraphClient | The name of the client class. Defaults to `Client`. |
| `--namespace-name \| -n` | No | Contoso.GraphApp | The namespace of the client class. Defaults to `Microsoft.Graph`. |
| `--backing-store \| -b` | No | | Defaults to `false` |
| `--exclude-backward-compatible \| --ebc` | No |  | Whether to exclude the code generated only for backward compatibility reasons or not. Defaults to `false`. |
| `--serializer \| -s` | No | `Contoso.Json.CustomSerializer` | One or more module names that implements ISerializationWriterFactory. Default are documented [here](https://learn.microsoft.com/openapi/kiota/using#--serializer--s). |
| `--deserializer \| --ds` | No | `Contoso.Json.CustomDeserializer` | One or more module names that implements IParseNodeFactory. Default are documented [here](https://learn.microsoft.com/en-us/openapi/kiota/using#--deserializer---ds). |
| `--structured-mime-types \| -m` | No | `application/json` |Any valid MIME type which will match a request body type or a response type in the OpenAPI description. Default are documented [here](https://learn.microsoft.com/en-us/openapi/kiota/using#--structured-mime-types--m). |
| `--output \| -o` | No | ./generated/graph/csharp | The output directory or file path for the generated code files. Defaults to `./output`. |

> [!NOTE] 
> It is not required to use the CLI to ad a new clients. It is possible to add a new client by adding a new entry in the `clients` section of the `kiota.config` file. See [kiota.config](#kiotaconfig) for more information.

#### Using kiota client add

```bash
kiota client add --api-name "graph" --client-name "graphDelegated" --language csharp --class-name "GraphClient" --namespace-name "Contoso.GraphApp" --backing-store --exclude-backward-compatible --serializer "Contoso.Json.CustomSerializer" --deserializer "Contoso.Json.CustomDeserializer" -structured-mime-types "application/json" --output "./generated/graph/csharp"
```

```jsonc
// Adds the following to the kiota.config file
"clients": {
  "graphDelegated": {
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
```

The resulting `kiota.config` file will look like this:

```jsonc
{
  "name": "My application",
  "apis": {
    "graph": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://.../openapi.yaml",
      "includePatterns": ["/me/chats#GET", "/me#GET"],
      "excludePatterns": [],
      "clients": {
        "graphDelegated": {
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

### kiota client delete

`kiota api client` allows a developer to delete an existing client from the `kiota.config` file. The command will remove the entry from the `clients` section of parent API within the `kiota.config` file. The command has two required parameters, the name of the API and the name of the client. The command also has one optional parameter, the ability to remove the generated client. If provided, kiota will delete the folder specified at the `outputPath` from the client configuration.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--api-name \| --api` | Yes | graph | Name of the API |
| `--client-name \| --cn` | Yes | graphDelegated | Name of the client |
| `--clean-output \| --co` | No |  | Cleans the generated client |

#### Using kiota client delete

```bash
kiota client delete --api-name "graph" --client-name "graphDelegated" --clean-output
```

```jsonc
// Removes the following from the kiota.config file
"graphDelegated": {
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
```

The resulting `kiota.config` file will look like this:

```jsonc
{
  "name": "My application",
  "version": "1.0",
  "apis": {
    "graph": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://.../openapi.yaml",
      "includePatterns": ["/me/chats#GET", "/me#GET"],
      "excludePatterns": [],
      "clients": { }
    }
  }
}
```

### kiota generate

Now that we have a `kiota.config` file, all the parameters required to generate the code are stored in the file. The `kiota generate` command will read the `kiota.config` file and generate the code for each of the clients. 

It's also possible to specify for which API and client the code should be generated. This is useful when a project contains multiple APIs and clients. The `kiota generate --api-name "MyAPI" --client-name "MyClient"` command will read the `kiota.config` file and generate the code for the specified API and client. If it can't find the specified API or client, it will throw an error.

In scenarios where a developer only needs a single API or doesn't want to go through the ceremony of executing `kiota init`, it's possible to use `kiota generate` and initialize a `kiota.config` file with the values coming from the command parameters. No breaking changes are required to the existing `kiota generate` command.

#### kiota generate Parameters

> [!INFO]
> This list is only the new parameters that `kiota generate` should support.

| Parameters | Required | Example | Description |
| -- | -- | -- | -- |
| `--app-name \| --an` | No | My application | Name of the application |
| `--api-name \| --api` | No | graph | Name of the API |
| `--client-name \| --cn` | No | graphDelegated | Name of the client. Unique within the parent API. |

#### Using `kiota generate` with all parameters

```bash
kiota generate --app-name "My Application" --api-name "graph" --client-name "graphDelegated" --openapi "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml" --include-path "/me/chats#GET" --include-path "/me#GET" --language csharp --class-name "GraphClient" --namespace-name "Contoso.GraphApp" --backing-store --exclude-backward-compatible --serializer "Contoso.Json.CustomSerializer" --deserializer "Contoso.Json.CustomDeserializer" -structured-mime-types "application/json" --output "./generated/graph/csharp"
```

```json
{
  "name": "My application",
  "apis": {
    "graph": {
      "descriptionHash": "9EDF8506CB74FE44...",
      "descriptionLocation": "https://.../openapi.yaml",
      "includePatterns": ["/me/chats#GET", "/me#GET"],
      "excludePatterns": [],
      "clients": {
        "graphDelegated": {
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

```json
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
          "clients": {
            "type": "object",
            "patternProperties": {
              ".*": {
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
                      "structuredMediaTypes": {
                        "type": "array",
                        "items": {
                          "type": "string"
                        }
                      },
                      "serializers": {
                        "type": "array",
                        "items": {
                          "type": "string"
                        }
                      },
                      "deserializers": {
                        "type": "array",
                        "items": {
                          "type": "string"
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
