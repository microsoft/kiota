package serialization

type Parsable interface {
	Serialize(writer SerializationWriter) error
}
