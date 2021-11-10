package jsonserialization

import (
	"errors"

	absser "github.com/microsoft/kiota/abstractions/go/serialization"
)

// The SerializationWriterFactory implementation for JSON serialization.
type JsonSerializationWriterFactory struct {
}

// Creates a new instance of the JsonSerializationWriterFactory.
func NewJsonSerializationWriterFactory() *JsonSerializationWriterFactory {
	return &JsonSerializationWriterFactory{}
}

func (f *JsonSerializationWriterFactory) GetValidContentType() (string, error) {
	return "application/json", nil
}
func (f *JsonSerializationWriterFactory) GetSerializationWriter(contentType string) (absser.SerializationWriter, error) {
	validType, err := f.GetValidContentType()
	if err != nil {
		return nil, err
	} else if contentType == "" {
		return nil, errors.New("contentType is empty")
	} else if contentType != validType {
		return nil, errors.New("contentType is not valid")
	} else {
		return NewJsonSerializationWriter(), nil
	}
}
