package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

// Registers the default serializer to the registry.
// Parameters:
// 		metaFactory: the factory function that creates the serialization writer factory.
func RegisterDefaultSerializer(metaFactory func() s.SerializationWriterFactory) {
	factory := metaFactory()
	contentType, err := factory.GetValidContentType()
	if err == nil && contentType != "" {
		s.DefaultSerializationWriterFactoryInstance.ContentTypeAssociatedFactories[contentType] = factory
	}
}

// Registers the default deserializer to the registry.
// Parameters:
// 		metaFactory: the factory function that creates the serialization reader factory.
func RegisterDefaultDeserializer(metaFactory func() s.ParseNodeFactory) {
	factory := metaFactory()
	contentType, err := factory.GetValidContentType()
	if err == nil && contentType != "" {
		s.DefaultParseNodeFactoryInstance.ContentTypeAssociatedFactories[contentType] = factory
	}
}
