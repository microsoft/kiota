package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

func RegisterDefaultSerializer(metaFactory func() s.SerializationWriterFactory) {
	factory := metaFactory()
	contentType, err := factory.GetValidContentType()
	if err == nil && contentType != "" {
		s.DefaultSerializationWriterFactoryInstance.ContentTypeAssociatedFactories[contentType] = factory
	}
}

func RegisterDefaultDeserializer(metaFactory func() s.ParseNodeFactory) {
	factory := metaFactory()
	contentType, err := factory.GetValidContentType()
	if err == nil && contentType != "" {
		s.DefaultParseNodeFactoryInstance.ContentTypeAssociatedFactories[contentType] = factory
	}
}
