package serialization

import "errors"

// implements the SerializationWriterFactory
type SerializationWriterFactoryRegistry struct {
	ContentTypeAssociatedFactories map[string]SerializationWriterFactory
}

var DefaultSerializationWriterFactoryInstance = SerializationWriterFactoryRegistry{
	ContentTypeAssociatedFactories: make(map[string]SerializationWriterFactory),
}

func (m *SerializationWriterFactoryRegistry) GetValidContentType() (string, error) {
	return "", errors.New("The registry supports multiple content types. Get the registered factory instead.")
}
func (m *SerializationWriterFactoryRegistry) GetSerializationWriter(contentType string) (SerializationWriter, error) {
	if contentType == "" {
		return nil, errors.New("The content type is empty")
	}
	factory := m.ContentTypeAssociatedFactories[contentType]
	if factory == nil {
		return nil, errors.New("Content type " + contentType + " does not have a factory registered to be parsed")
	} else {
		return factory.GetSerializationWriter(contentType)
	}
}
