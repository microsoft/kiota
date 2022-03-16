package textserialization

import (
	assert "github.com/stretchr/testify/assert"
	"testing"
)

func TestItDoesntWriteAnythingForAdditionalData(t *testing.T) {
	serializer := NewTextSerializationWriter()
	err := serializer.WriteAdditionalData(nil)
	assert.NotNil(t, err)
}
