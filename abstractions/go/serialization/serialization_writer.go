package serialization

import (
	i "io"
)

type SerializationWriter interface {
	i.Closer
	WritePrimitiveValue(key string, value interface{}) error
	WriteObjectValue(key string, item Parsable) error
	WriteCollectionOfObjectValues(key string, collection []Parsable) error
	WriteCollectionOfPrimitiveValues(key string, collection []interface{}) error
	GetSerializedContent() ([]byte, error)
	WriteAdditionalData(value map[string]interface{}) error
}

func ConvertToArrayOfParsable(params ...interface{}) []Parsable {
	var result []Parsable
	for _, param := range params {
		result = append(result, param.(Parsable))
	}
	return result
}
func ConvertToArrayOfPrimitives(params ...interface{}) []interface{} {
	var result []interface{}
	for _, param := range params {
		result = append(result, param)
	}
	return result
}
