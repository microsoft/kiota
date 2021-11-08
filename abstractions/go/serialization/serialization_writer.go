package serialization

import (
	i "io"
	"time"

	"github.com/google/uuid"
)

// Defines an interface for serialization of objects to a byte array.
type SerializationWriter interface {
	i.Closer
	// Writes a String value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the String value to write.
	// Returns:
	// - An error if any.
	WriteStringValue(key string, value *string) error
	// Writes a Bool value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the Bool value to write.
	// Returns:
	// - An error if any.
	WriteBoolValue(key string, value *bool) error
	// Writes a Int32 value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the Int32 value to write.
	// Returns:
	// - An error if any.
	WriteInt32Value(key string, value *int32) error
	// Writes a Int64 value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the Int64 value to write.
	// Returns:
	// - An error if any.
	WriteInt64Value(key string, value *int64) error
	// Writes a Float32 value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the Float32 value to write.
	// Returns:
	// - An error if any.
	WriteFloat32Value(key string, value *float32) error
	// Writes a Float64 value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the Float64 value to write.
	// Returns:
	// - An error if any.
	WriteFloat64Value(key string, value *float64) error
	// Writes a ByteArray value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the ByteArray value to write.
	// Returns:
	// - An error if any.
	WriteByteArrayValue(key string, value []byte) error
	// Writes a Time value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the Time value to write.
	// Returns:
	// - An error if any.
	WriteTimeValue(key string, value *time.Time) error
	// Writes a UUID value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the UUID value to write.
	// Returns:
	// - An error if any.
	WriteUUIDValue(key string, value *uuid.UUID) error
	// Writes a Parsable value to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - value - the Parsable value to write.
	// Returns:
	// - An error if any.
	WriteObjectValue(key string, item Parsable) error
	// Writes a collection of Parsable values to the byte array.
	// Parameters:
	// - key - the key of the value to write (optional).
	// - collection - the collection of Parsable value to write.
	// Returns:
	// - An error if any.
	WriteCollectionOfObjectValues(key string, collection []Parsable) error
	// Writes a collection of String values to the byte array.
	// Parameters:
	// - key - the key to write (optional).
	// - collection - the collection to write.
	// Returns:
	// - An error if any.
	WriteCollectionOfStringValues(key string, collection []string) error
	// Writes a collection of Bool values to the byte array.
	// Parameters:
	// - key - the key to write (optional).
	// - collection - the collection to write.
	// Returns:
	// - An error if any.
	WriteCollectionOfBoolValues(key string, collection []bool) error
	// Writes a collection of Int32 values to the byte array.
	// Parameters:
	// - key - the key to write (optional).
	// - collection - the collection to write.
	// Returns:
	// - An error if any.
	WriteCollectionOfInt32Values(key string, collection []int32) error
	// Writes a collection of Int64 values to the byte array.
	// Parameters:
	// - key - the key to write (optional).
	// - collection - the collection to write.
	// Returns:
	// - An error if any.
	WriteCollectionOfInt64Values(key string, collection []int64) error
	// Writes a collection of Float32 values to the byte array.
	// Parameters:
	// - key - the key to write (optional).
	// - collection - the collection to write.
	// Returns:
	// - An error if any.
	WriteCollectionOfFloat32Values(key string, collection []float32) error
	// Writes a collection of Float64 values to the byte array.
	// Parameters:
	// - key - the key to write (optional).
	// - collection - the collection to write.
	// Returns:
	// - An error if any.
	WriteCollectionOfFloat64Values(key string, collection []float64) error
	// Writes a collection of Time values to the byte array.
	// Parameters:
	// - key - the key to write (optional).
	// - collection - the collection to write.
	// Returns:
	// - An error if any.
	WriteCollectionOfTimeValues(key string, collection []time.Time) error
	// Writes a collection of UUID values to the byte array.
	// Parameters:
	// - key - the key to write (optional).
	// - collection - the collection to write.
	// Returns:
	// - An error if any.
	WriteCollectionOfUUIDValues(key string, collection []uuid.UUID) error
	// Gets the resulting byte array from the serialization writer.
	// Returns:
	// - The resulting byte array.
	// - An error if any.
	GetSerializedContent() ([]byte, error)
	// Writes additional data to the byte array.
	// Parameters:
	// - value - the data to write.
	// Returns:
	// - An error if any.
	WriteAdditionalData(value map[string]interface{}) error
}
