---
parent: Understand and Extend the Kiota generator
---

# Models

The primary goal of models is to enable a developer to easily craft request payloads and get result as high levels objects without having to define their own types and/or implementation serialization/deserialization themselves.

## Components versus inline models

All models declared as components will be generated under a `models` sub-namespace. If models/components are namespaced `components/modelA`, `components/ns1/modelB`, the namespaces will be included as sub-namespaces of the `models` namespace.

All models declared inline with an operation will be generated under the namespace of the operation, next to the request builder it is used by. As a reminder, all request builders are put in namespaces according to the path segment the refer to.

## Inheritance

Models in an [AllOf](https://spec.openapis.org/oas/latest.html#composition-and-inheritance-polymorphism) schema declaration will inherit from each other. Where the uppermost type in the collection is the greatest ancestor of the chain.

## Faceted implementation of OneOf

OneOf specifies a type exclusion where the response can be of One of the specified schemas. Kiota implements that specification by generating all the target types and using a union type for languages that support it or a wrapper type with one property per type in the union for languages that do not support union types.

The deserialized result will either be of the one of the types of the union or be of the wrapper type with only one of the properties being non null.

## Faceted implementation of AnyOf

OneOf specifies a type inclusion where the response can be of Any of the specified schemas. Kiota implements that specification by generating all the target types and using a union type for languages that support it or a wrapper type with one property per type in the union for languages that do not support union types.

The deserialized result will either be of the one of the types of the union or be of the wrapper type with one or more of the properties being non null.

## Heterogeneous collections

For any collection of items that rely on AllOf, AnyOf, or OneOf, it is possible the result will contain multiple types of objects.

For example, think of an endpoint returning a collection of directory objects (abstract). Directory object is derived by User and Group, which each have their own set of properties. In this case the endpoint will be documented as returning a collection of directory objects and return in reality a mix of users and groups.

Kiota [has plans](https://github.com/microsoft/kiota/issues/648) to support discriminators, down-casting the returned object during deserialization, however the work on this aspect is currently blocked by work required in dependencies. Kiota will currently return everything as the described type. Properties from the child types can be accessed from the additional data property.

## Default members

In addition to all the described properties, a model will contain a set of default members.

### Field deserializers

The field deserializers method or property contains a list of callbacks to be used by the `ParseNode` implementation when deserializing the objects. Kiota relies on auto-serialization, where each type *knows* how to serialize/deserialize itself thanks to the OpenAPI description. A big advantage of this approach it to avoid tying the generated models to any specific serialization format (JSON, YAML, XML,...) or any specific library (because of attributes/annotations these libraries often require).

### Serialize method

Like the field deserializers, the model's serialize method leverages the passed `SerializationWriter` to serialize itself.

### Additional data

Dictionary/Map that stores all the additional properties which are not described in the schema.

> Note: the additional data property is only present when the OpenAPI description for the type allows it and if the current model doesn't inherit a model which already has this property.

### Backing store

When present, the properties values are store in this backing store instead of using fields for the object. The backing store allows multiple things like dirty tracking of changes, making it possible to get an object from the API, update a property, send that object back with only the changed property and not the full objects. Additionally it will be used for integration with third party data sources.

> Note: the backing store is only added if the target language supports it and when the `-b` parameter is passed to the CLI when generating the models.
