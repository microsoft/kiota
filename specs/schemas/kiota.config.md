## JSON Schema for kiota-config.json

```jsonc
{
  "$schema": "<http://json-schema.org/draft-07/schema#>",
  "type": "object",
  "properties": {
    "version": {
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