package serialization

import (
	"time"

	"github.com/google/uuid"
)

type ParseNode interface {
	GetChildNode(index string) (ParseNode, error)
	GetCollectionOfObjectValues(ctor func() Parsable) ([]Parsable, error)
	GetCollectionOfPrimitiveValues(targetType string) ([]interface{}, error)
	GetCollectionOfEnumValues(parser func(string) (interface{}, error)) ([]interface{}, error)
	GetObjectValue(ctor func() Parsable) (Parsable, error)
	GetStringValue() (*string, error)
	GetBoolValue() (*bool, error)
	GetFloat32Value() (*float32, error)
	GetFloat64Value() (*float64, error)
	GetInt32Value() (*int32, error)
	GetInt64Value() (*int64, error)
	GetTimeValue() (*time.Time, error)
	GetUUIDValue() (*uuid.UUID, error)
	GetEnumValue(parser func(string) (interface{}, error)) (interface{}, error)
	GetByteArrayValue() ([]byte, error)
}
