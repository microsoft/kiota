package serialization

// Parsable defines a serializable model object.
type Parsable interface {
	// Serialize writes the objects properties to the current writer.
	Serialize(writer SerializationWriter) error
	// GetFieldDeserializers returns the deserialization information for this object.
	GetFieldDeserializers() map[string]func(interface{}, ParseNode) error
	// IsNil returns whether the current object is nil or not.
	IsNil() bool
}

// ParsableFactory is a factory for creating Parsable.
type ParsableFactory func(parseNode ParseNode) (Parsable, error)
