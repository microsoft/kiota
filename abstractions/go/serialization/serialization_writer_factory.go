package serialization

type SerializationWriterFactory interface {
	GetValidContentType() (string, error)
	GetSerializationWriter(contentType string) (SerializationWriter, error)
}
