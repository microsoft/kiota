package abstractions

import (
	sync "sync"

	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

var serializerMutex sync.Mutex
var deserializerMutex sync.Mutex

// RegisterDefaultSerializer registers the default serializer to the registry singleton to be used by the request adapter.
func RegisterDefaultSerializer(metaFactory func() s.SerializationWriterFactory) {
	factory := metaFactory()
	contentType, err := factory.GetValidContentType()
	if err == nil && contentType != "" {
		serializerMutex.Lock()
		s.DefaultSerializationWriterFactoryInstance.ContentTypeAssociatedFactories[contentType] = factory
		serializerMutex.Unlock()
	}
}

// RegisterDefaultDeserializer registers the default deserializer to the registry singleton to be used by the request adapter.
func RegisterDefaultDeserializer(metaFactory func() s.ParseNodeFactory) {
	factory := metaFactory()
	contentType, err := factory.GetValidContentType()
	if err == nil && contentType != "" {
		deserializerMutex.Lock()
		s.DefaultParseNodeFactoryInstance.ContentTypeAssociatedFactories[contentType] = factory
		deserializerMutex.Unlock()
	}
}
