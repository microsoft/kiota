package serialization

import (
	"time"

	"github.com/google/uuid"
)

type ParseNode interface {
	GetChildNode(index string) (ParseNode, error)
	GetCollectionOfObjectValues(func() interface{}) ([]Parsable, error)
	GetCollectionOfPrimitiveValues(targetType string) ([]interface{}, error)
	GetObjectValue(func() interface{}) (Parsable, error)
	GetStringValue() (*string, error)
	GetBoolValue() (*bool, error)
	GetFloat32Value() (*float32, error)
	GetInt32Value() (*int32, error)
	GetInt64Value() (*int64, error)
	GetTimeValue() (*time.Time, error)
	GetUUIDValue() (*uuid.UUID, error)
	GetEnumValue(func(string) (interface{}, error)) (interface{}, error)
}
