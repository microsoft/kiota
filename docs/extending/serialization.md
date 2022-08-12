---
parent: Kiota deep dive
---

# Serialization with Kiota clients

APIs rely on some serialization format (JSON, YAML, XML...) to be able to receive and respond to requests from their clients. Kiota generated models and request builders are not tied to any specific serialization format or implementation library. To achieve this Kiota generated clients rely on two key concepts:

- Models rely on a set of abstractions described below, available in the abstractions package and implemented in a separate package for each format.
- Models self-describe their serialization/deserialization logic, more information in [the models](./models.md) documentation page.

## Additional Data Holder

The additional data holder interface defines members implemented by models when they support additional properties that are not described in the OpenAPI description of the API but could be part of the response.

```csharp
public interface IAdditionalDataHolder
{
    IDictionary<string, object> AdditionalData { get; set; }
}

```

## Parsable

The parsable interface defines members that are required to be implemented by a model in order to be able to self serialize/deserialize itself. You can find a detailed description of those members in the [models](./models.md) documentation page.

```csharp
public interface IParsable
{
    IDictionary<string, Action<T, IParseNode>> GetFieldDeserializers<T>();
    void Serialize(ISerializationWriter writer);
    IDictionary<string, object> AdditionalData { get; set; }
}
```

## Parsable Factory

Parsable Factory defines the structure for models factories creating parsable objects according or their derived types to the discriminator value present in the payload.

```CSharp
public delegate T ParsableFactory<T>(IParseNode node) where T : IParsable;
```

## Parse Node

The parse node interface defines members that are required to be implemented by a deserialization library. It mostly acts as an abstractions and mapping layer between the methods models' GetFieldDeserializers will call and the library in use to deserialize the payload. It heavily relies on recurrence programming design and the implementing class will be instantiated by a corresponding factory.

On top of the mapping methods, parse node offers two events that must be called during the deserialization process by implementers:

- OnBeforeAssignFieldValues: when the target object has been created and before fields of the object have been assigned.
- OnAfterAssignFieldValues: when the target object field values have been assigned.

## Parse Node Factory

The parse node factory interface defines members that need to be implemented to instantiate a new parse node implementer from a raw payload as well as describe which mime type this factory/parse node applies to.

Whenever the request adapter needs to deserialize a response body, it will look for a factory that applies to that result based on the HTTP Response content type header, instantiate a new parse node for the corresponding type using the factory, and call in the parse node to get the deserialized result.

## Parse Node Factory Factory Registry

This class is a registry for multiple parse node factories which can be used by the request adapter. Implementers for new deserialization libraries or formats do not need to do anything with this type other than requesting users to register their new factory at runtime with the provided singleton to make is available for the Kiota client.

## Parse Node Factory Proxy Factory

Parse node offers multiple *events* that are called during the deserialization sequence to allow a third party to be notified. This proxy facilitate the registration of such events with existing parse nodes. Implementers for new deserialization libraries or formats do not need to do anything with this type other than requesting users to wrap their factories with it as well should they be subscribing to deserialization events.

## Serialization Writer

The serialization writer interface defines members that are required to be implemented by a serialization library. It mostly acts as an abstractions and mapping layer between the methods models' Serialize will call and the library in use to serialize the payload. It relies on the implementing class will be instantiated by a corresponding factory.

On top of the mapping methods, serialization writer offers two events that must be called during the serialization process by implementers:

- OnBeforeObjectSerialization: called before the serialization process starts.
- OnStartObjectSerialization: called right after the serialization process starts.
- OnAfterObjectSerialization: called after the serialization process ends.

## Serialization Writer Factory

The serialization writer factory interface defines members that need to be implemented to instantiate a new serialization writer implementer as well as describe which mime type this factory/serialization writer applies to.

Whenever the request adapter needs to serialize a request body, it will look for a factory that applies to that result based on the HTTP Request content type header, instantiate a serialization writer for the corresponding type using the factory, and call in the serialization writer to get the serialized result.

## Serialization Writer Factory Registry

This class is a registry for multiple serialization writer factories which can be used by the request adapter. Implementers for new serialization libraries or formats do not need to do anything with this type other than requesting users to register their new factory at runtime with the provided singleton to make is available for the Kiota client.

## Serialization Writer Proxy Factory

Serialization writer offers multiple *events* that are called during the serialization sequence to allow a third party to be notified. This proxy facilitate the registration of such events with existing serialization writers. Implementers for new serialization libraries or formats do not need to do anything with this type other than requesting users to wrap their factories with it as well should they be subscribing to serialization events.
