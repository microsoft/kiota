---
parent: Kiota deep dive
---

# Models

The primary goal of models is to enable a developer to easily craft request payloads and get result as high levels objects without having to define their own types and/or implementation serialization/deserialization themselves.

## Components versus inline models

All models declared as components will be generated under a `models` sub-namespace. If models/components are namespaced `components/modelA`, `components/ns1/modelB`, the namespaces will be included as sub-namespaces of the `models` namespace.

All models declared inline with an operation will be generated under the namespace of the operation, next to the request builder it is used by. As a reminder, all request builders are put in namespaces according to the path segment the refer to.

## Inheritance

Models in an [AllOf](https://spec.openapis.org/oas/latest.html#composition-and-inheritance-polymorphism) schema declaration will inherit from each other. Where the uppermost type in the collection is the greatest ancestor of the chain.

## Faceted implementation of oneOf

oneOf specifies a type union (exclusive) where the response can be of one of the specified child schemas. Kiota implements that specification by generating types for all the child schemas and using a union type for languages that support it or a wrapper type with one property per type in the union.

The deserialized result will either be of the one of the types of the union or be of the wrapper type with only one of the properties being non null.

When a oneOf keyword has at least one child schema that is of type object then the OpenApi discriminator keyword MUST be provided to identify the applicable schema.  

Child schemas that are arrays or primitives will use the equivalent type language parser to attempt to interpret the input value. The first primitive schema that does not fail to parse will be used to deserialize the input.  

Nested oneOf keywords are only supported when the child schema uses a `$ref` to enable naming the nested type.

## Faceted implementation of anyOf

anyOf specifies a type intersection (inclusive union) where the response can be of any of the specified child schemas. Kiota implements that specification by generating types for all the child schemas and using a intersection type for languages that support it or a wrapper type with one property per type in the union.

The deserialized result will either intersection type or be of the wrapper type with one or more of the properties being non null.

Where there are common properties in the child schemas, the corresponding value in the input will deserialized into first the child schemas with the common property.

## Heterogeneous collections

For any collection of items that rely on AllOf, AnyOf, or OneOf, it is possible the result will contain multiple types of objects.

For example, think of an endpoint returning a collection of directory objects (abstract). Directory object is derived by User and Group, which each have their own set of properties. In this case the endpoint will be documented as returning a collection of directory objects and return in reality a mix of users and groups.

Kiota supports discriminators by down-casting the returned object during deserialization. The down-casting is supported through the use of allOf and discriminator property. Kiota supports both implicit and explicit discriminator mappings (example 1). Using oneOf to constrain derived types is **not** supported (example 2) as it will be interpreted as an intersection type.

In case of inline schemas, the type will be named by conventions:

- Endpoint name + Operation + RequestBody
- Endpoint name + Operation + Response
- Parent type name + member + id (sequential)

### Example 1 - using allOf and discriminator

```json
{
    "microsoft.graph.directoryObject": {
      "allOf": [
        { "$ref": "#/components/schemas/microsoft.graph.entity"},
        {
            "title": "directoryObject",
            "required": [
                "@odata.type"
            ],
            "type": "object",
            "properties": {
                "@odata.type": {
                    "type": "string",
                    "default": "#microsoft.graph.directoryObject"
                }
            }
        }
      ],
      "discriminator": {
            "propertyName": "@odata.type",
            "mapping": {
              "#microsoft.graph.user": "#/components/schemas/microsoft.graph.user",
              "#microsoft.graph.group": "#/components/schemas/microsoft.graph.group"
            }
        }
    },
    "microsoft.graph.user": {
      "allOf": [
        { "$ref": "#/components/schemas/microsoft.graph.directoryObject"},
        {
            "title": "user",
            "type": "object"
        }
      ]
    },
    "microsoft.graph.group": {
      "allOf": [
        { "$ref": "#/components/schemas/microsoft.graph.directoryObject"},
        {
            "title": "group",
            "type": "object"
        }
      ]
    },
}
```

### Example 2 - using oneOf to constrain the derived types - not supported

```json
{
    "type": "object",
    "title": "directoryObject",
    "oneOf": [
        {
            "type": "object",
            "title": "user"
        },
        {
            "type": "object",
            "title": "group"
        }

    ]
}
```

## Default members

In addition to all the described properties, a model will contain a set of default members.

### Factory method

The Factory method (static) is used by the Parse Node implementation to get the base or derived instance according to the discriminator value. If an operation describes returning a Person model, and the Person model has discriminator information (mapping + property name), and the response payload contains one of the mapped value (e.g. Employee), the deserialization process will deserialize the the derived Employee type instead of the base Person type. This way SDK users can take advantage of the properties that are defined on this specialized model.

```csharp
public static new Person CreateFromDiscriminatorValue(IParseNode parseNode) {
    _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
    var mappingValueNode = parseNode.GetChildNode("@odata.type");
    var mappingValue = mappingValueNode?.GetStringValue();
    return mappingValue switch {
        "#api.Employee" => new Employee(),
        _ => new Person(),
    };
}
```

### Field deserializers

The field deserializers method or property contains a list of callbacks to be used by the `ParseNode` implementation when deserializing the objects. Kiota relies on auto-serialization, where each type *knows* how to serialize/deserialize itself thanks to the OpenAPI description. A big advantage of this approach it to avoid tying the generated models to any specific serialization format (JSON, YAML, XML,...) or any specific library (because of attributes/annotations these libraries often require).

### Serialize method

Like the field deserializers, the model's serialize method leverages the passed `SerializationWriter` to serialize itself.

### Additional data

Dictionary/Map that stores all the additional properties which are not described in the schema.

> **Note:** the additional data property is only present when the OpenAPI description for the type allows it and if the current model doesn't inherit a model which already has this property.

### Backing store

When present, the properties values are store in this backing store instead of using fields for the object. The backing store allows multiple things like dirty tracking of changes, making it possible to get an object from the API, update a property, send that object back with only the changed property and not the full objects. Additionally it will be used for integration with third party data sources.

> **Note:** the backing store is only added if the target language supports it and when the `-b` parameter is passed to the CLI when generating the models.
