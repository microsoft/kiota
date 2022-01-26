package jsonserialization

import (
	assert "github.com/stretchr/testify/assert"
	"testing"
)

func TestItDoesntWriteAnythingForNilAdditionalData(t *testing.T) {
	serializer := NewJsonSerializationWriter()
	serializer.WriteAdditionalData(nil)
	result, err := serializer.GetSerializedContent()
	assert.Nil(t, err)
	assert.Equal(t, 0, len(result))
}

func TestItDoesntWriteAnythingForEmptyAdditionalData(t *testing.T) {
	serializer := NewJsonSerializationWriter()
	serializer.WriteAdditionalData(make(map[string]interface{}))
	result, err := serializer.GetSerializedContent()
	assert.Nil(t, err)
	assert.Equal(t, 0, len(result))
}

func TestItDoesntTrimCommasOnEmptyAdditionalData(t *testing.T) {
	serializer := NewJsonSerializationWriter()
	value := "value"
	serializer.WriteStringValue("key", &value)
	serializer.WriteAdditionalData(make(map[string]interface{}))
	value2 := "value2"
	serializer.WriteStringValue("key2", &value2)
	result, err := serializer.GetSerializedContent()
	assert.Nil(t, err)
	assert.Equal(t, "\"key\":\"value\",\"key2\":\"value2\",", string(result[:]))
}
