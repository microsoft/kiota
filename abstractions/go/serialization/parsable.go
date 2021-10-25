package serialization

// Defines a serializable model object.
type Parsable interface {
	//  Writes the objects properties to the current writer.
	// Parameters:
	//  - writer: the writer to write the properties to.
	// Returns:
	//  - error: the error that occurred while writing the properties.
	Serialize(writer SerializationWriter) error
	//   Gets the deserialization information for this object.
	// Returns:
	//   The deserialization information for this object.
	GetFieldDeserializers() map[string]func(interface{}, ParseNode) error
	// Sets additional data of the object that doesn't belong to a field.
	// Parameters:
	//  - data: the data to set.
	SetAdditionalData(value map[string]interface{})
	// Gets additional data of the object that doesn't belong to a field.
	// Returns:
	//  - data: the data.
	GetAdditionalData() map[string]interface{}
	// Returns whether the current object is nil or not.
	IsNil() bool
}
