package serialization

import (
	assert "github.com/stretchr/testify/assert"
	"testing"
    "github.com/stretchr/testify/mock"
)
type mockSerializer struct {
}

func (*mockSerializer) WriteStringValue(key string, value *string) error {
	return nil
}
func (*mockSerializer) WriteBoolValue(key string, value *bool) error {
	return nil
}
func (*mockSerializer) WriteByteValue(key string, value *byte) error {
	return nil
}
func (*mockSerializer) WriteInt8Value(key string, value *int8) error {
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
func (*mockSerializer) WriteISODurationValue(key string, value *serialization.ISODuration) error {
	return nil
}
func (*mockSerializer) WriteDateOnlyValue(key string, value *serialization.DateOnly) error {
	return nil
}
func (*mockSerializer) WriteTimeOnlyValue(key string, value *serialization.TimeOnly) error {
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
func (*mockSerializer) WriteCollectionOfByteValues(key string, collection []byte) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfInt8Values(key string, collection []int8) error {
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
func (*mockSerializer) WriteCollectionOfISODurationValues(key string, collection []serialization.ISODuration) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfDateOnlyValues(key string, collection []serialization.DateOnly) error {
	return nil
}
func (*mockSerializer) WriteCollectionOfTimeOnlyValues(key string, collection []serialization.TimeOnly) error {
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

func TestItGetsVendorSpecificSerializationWriter(t *testing.T) {
    registry := NewSerializationWriterFactoryRegistry()
    registry.ContentTypeAssociatedFactories["application/json"] = &mockSerializerFactory{}
    serializationWriter = registry.GetSerializationWriter("application/vnd+json")
    assert.NotNil(t, serializationWriter)
}
