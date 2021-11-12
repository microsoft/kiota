package serialization

import (
	"time"

	"github.com/google/uuid"
)

// Interface for a deserialization node in a parse tree. This interace provides an abstraction layer over serialiation formats, libararies and implementations.
type ParseNode interface {
	// Gets a new parse node for the given identifier.
	// Parameters:
	//  - index: The identifier of the new node.
	// Returns:
	//  - The new node.
	//  - An error if any
	GetChildNode(index string) (ParseNode, error)
	// Gets the collection of Parsable values from the node.
	// Parameters:
	// - ctor: the factory for the target type of parsable
	// Returns:
	//  - The collection of parsable values.
	//  - An error if any
	GetCollectionOfObjectValues(ctor func() Parsable) ([]Parsable, error)
	// Gets the collection of primitive values from the node.
	// Parameters:
	// - targetType: the target primitive type
	// Returns:
	//  - The collection of primitive values.
	//  - An error if any
	GetCollectionOfPrimitiveValues(targetType string) ([]interface{}, error)
	// Gets the collection of Enum values from the node.
	// Parameters:
	// - parser: the parser method for the target enum
	// Returns:
	//  - The collection of enum values.
	//  - An error if any
	GetCollectionOfEnumValues(parser func(string) (interface{}, error)) ([]interface{}, error)
	// Gets the Parsable value from the node.
	// Parameters:
	// - ctor: the factory for the target type of parsable
	// Returns:
	//  - The parsable values.
	//  - An error if any
	GetObjectValue(ctor func() Parsable) (Parsable, error)
	// Gets a String value from the nodes.
	// Returns:
	// - A String value when available
	// - An error if any
	GetStringValue() (*string, error)
	// Gets a Bool value from the nodes.
	// Returns:
	// - A Bool value when available
	// - An error if any
	GetBoolValue() (*bool, error)
	// Gets a Float32 value from the nodes.
	// Returns:
	// - A Float32 value when available
	// - An error if any
	GetFloat32Value() (*float32, error)
	// Gets a Float64 value from the nodes.
	// Returns:
	// - A Float64 value when available
	// - An error if any
	GetFloat64Value() (*float64, error)
	// Gets a Int32 value from the nodes.
	// Returns:
	// - A Int32 value when available
	// - An error if any
	GetInt32Value() (*int32, error)
	// Gets a Int64 value from the nodes.
	// Returns:
	// - A Int64 value when available
	// - An error if any
	GetInt64Value() (*int64, error)
	// Gets a Time value from the nodes.
	// Returns:
	// - A Time value when available
	// - An error if any
	GetTimeValue() (*time.Time, error)
	// Gets a UUID value from the nodes.
	// Returns:
	// - A UUID value when available
	// - An error if any
	GetUUIDValue() (*uuid.UUID, error)
	// Gets a Enum value from the nodes.
	// Returns:
	// - A Enum value when available
	// - An error if any
	GetEnumValue(parser func(string) (interface{}, error)) (interface{}, error)
	// Gets a ByteArray value from the nodes.
	// Returns:
	// - A ByteArray value when available
	// - An error if any
	GetByteArrayValue() ([]byte, error)
}
