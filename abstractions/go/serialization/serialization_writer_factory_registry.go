package serialization

import "errors"

//  This factory holds a list of all the registered factories for the various types of nodes.
type SerializationWriterFactoryRegistry struct {
	// List of factories that are registered by content type.
	ContentTypeAssociatedFactories map[string]SerializationWriterFactory
}

// Default singleton instance of the registry to be used when registring new factories that should be available by default.
var DefaultSerializationWriterFactoryInstance = &SerializationWriterFactoryRegistry{
	ContentTypeAssociatedFactories: make(map[string]SerializationWriterFactory),
}

// The valid content type for the SerializationWriterFactoryRegistry
// Returns:
// 		- the content type
// 		- nil if the content type is valid
func (m *SerializationWriterFactoryRegistry) GetValidContentType() (string, error) {
	return "", errors.New("the registry supports multiple content types. Get the registered factory instead")
}

// Get the relevant SerializationWriter instance for the given content type
// Parameters:
// 		- contentType: the content type
// Returns:
// 		- the SerializationWriter instance
//      - an error if any
func (m *SerializationWriterFactoryRegistry) GetSerializationWriter(contentType string) (SerializationWriter, error) {
	if contentType == "" {
		return nil, errors.New("the content type is empty")
	}
	factory := m.ContentTypeAssociatedFactories[contentType]
	if factory == nil {
		return nil, errors.New("Content type " + contentType + " does not have a factory registered to be parsed")
	} else {
		return factory.GetSerializationWriter(contentType)
	}
}
