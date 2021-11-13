package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

// RegisterDefaultSerializer registers the default serializer to the registry singleton to be used by the request adapter.
func RegisterDefaultSerializer(metaFactory func() s.SerializationWriterFactory) {
	factory := metaFactory()
	contentType, err := factory.GetValidContentType()
	if err == nil && contentType != "" {
		s.DefaultSerializationWriterFactoryInstance.ContentTypeAssociatedFactories[contentType] = factory
	}
}

// RegisterDefaultDeserializer registers the default deserializer to the registry singleton to be used by the request adapter.
func RegisterDefaultDeserializer(metaFactory func() s.ParseNodeFactory) {
	factory := metaFactory()
	contentType, err := factory.GetValidContentType()
	if err == nil && contentType != "" {
		s.DefaultParseNodeFactoryInstance.ContentTypeAssociatedFactories[contentType] = factory
	}
}
