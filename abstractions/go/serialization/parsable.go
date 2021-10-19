package serialization

type Parsable interface {
	Serialize(writer SerializationWriter) error
	GetFieldDeserializers() map[string]func(interface{}, ParseNode) error
	SetAdditionalData(value map[string]interface{})
	GetAdditionalData() map[string]interface{}
	IsNil() bool
}
