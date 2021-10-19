package serialization

import (
	i "io"
	"time"

	"github.com/google/uuid"
)

type SerializationWriter interface {
	i.Closer
	WriteStringValue(key string, value *string) error
	WriteBoolValue(key string, value *bool) error
	WriteInt32Value(key string, value *int32) error
	WriteInt64Value(key string, value *int64) error
	WriteFloat32Value(key string, value *float32) error
	WriteFloat64Value(key string, value *float64) error
	WriteByteArrayValue(key string, value []byte) error
	WriteTimeValue(key string, value *time.Time) error
	WriteUUIDValue(key string, value *uuid.UUID) error
	WriteObjectValue(key string, item Parsable) error
	WriteCollectionOfObjectValues(key string, collection []Parsable) error
	WriteCollectionOfStringValues(key string, collection []string) error
	WriteCollectionOfBoolValues(key string, collection []bool) error
	WriteCollectionOfInt32Values(key string, collection []int32) error
	WriteCollectionOfInt64Values(key string, collection []int64) error
	WriteCollectionOfFloat32Values(key string, collection []float32) error
	WriteCollectionOfFloat64Values(key string, collection []float64) error
	WriteCollectionOfTimeValues(key string, collection []time.Time) error
	WriteCollectionOfUUIDValues(key string, collection []uuid.UUID) error
	GetSerializedContent() ([]byte, error)
	WriteAdditionalData(value map[string]interface{}) error
}
