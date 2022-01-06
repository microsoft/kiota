package abstractions

import (
	"github.com/google/uuid"
	serialization "github.com/microsoft/kiota/abstractions/go/serialization"
	assert "github.com/stretchr/testify/assert"
	"testing"
	"time"
)

type mockSerializer struct {
}

func (*mockSerializer) WriteStringValue(key string, value *string) error {
	return nil
}
func (*mockSerializer) WriteBoolValue(key string, value *bool) error {
	return nil
}
func (*mockSerializer) WriteInt32Value(key string, value *int32) error {
	return nil
}
func (*mockSerializer) WriteInt64Value(key string, value *int64) error {
	return nil
}
func (*mockSerializer) WriteFloat32Value(key string, value *float32) error {
	return nil
}
func (*mockSerializer) WriteFloat64Value(key string, value *float64) error {
	return nil
}
func (*mockSerializer) WriteByteArrayValue(key string, value []byte) error {
	return nil
}
func (*mockSerializer) WriteTimeValue(key string, value *time.Time) error {
	return nil
}
func (*mockSerializer) WriteUUIDValue(key string, value *uuid.UUID) error {
	return nil
}
func (*mockSerializer) WriteObjectValue(key string, item serialization.Parsable) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfObjectValues(key string, collection []serialization.Parsable) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfStringValues(key string, collection []string) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfBoolValues(key string, collection []bool) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfInt32Values(key string, collection []int32) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfInt64Values(key string, collection []int64) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfFloat32Values(key string, collection []float32) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfFloat64Values(key string, collection []float64) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfTimeValues(key string, collection []time.Time) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfUUIDValues(key string, collection []uuid.UUID) error {
	return nil
}
func (*mockSerializer) GetSerializedContent() ([]byte, error) {
	return nil, nil
}
func (*mockSerializer) WriteAdditionalData(value map[string]interface{}) error {
	return nil
}
func (*mockSerializer) Close() error {
	return nil
}

type mockSerializerFactory struct {
}

func (*mockSerializerFactory) GetValidContentType() (string, error) {
	return "application/json", nil
}
func (*mockSerializerFactory) GetSerializationWriter(contentType string) (serialization.SerializationWriter, error) {
	return &mockSerializer{}, nil
}

func TestItCreatesClientConcurrently(t *testing.T) {
	metaFactory := func() serialization.SerializationWriterFactory {
		return &mockSerializerFactory{}
	}
	for i := 0; i < 1000; i++ {
		go RegisterDefaultSerializer(metaFactory)
	}
	assert.Equal(t, 1, len(serialization.DefaultSerializationWriterFactoryInstance.ContentTypeAssociatedFactories))
}
