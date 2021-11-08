package serialization

// Defines the contract for a factory that creates SerializationWriter instances.
type SerializationWriterFactory interface {
	// The valid content type for the SerializationWriterFactoryRegistry
	// Returns:
	// 		- the content type
	// 		- nil if the content type is valid
	GetValidContentType() (string, error)
	// Get the relevant SerializationWriter instance for the given content type
	// Parameters:
	// 		- contentType: the content type
	// Returns:
	// 		- the SerializationWriter instance
	//      - an error if any
	GetSerializationWriter(contentType string) (SerializationWriter, error)
}
