package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

type RequestAdapter interface {
	SendAsync(requestInfo RequestInformation, constructor func() s.Parsable, responseHandler ResponseHandler) (s.Parsable, error)
	SendCollectionAsync(requestInfo RequestInformation, constructor func() s.Parsable, responseHandler ResponseHandler) ([]s.Parsable, error)
	SendPrimitiveAsync(requestInfo RequestInformation, typeName string, responseHandler ResponseHandler) (interface{}, error)
	SendPrimitiveCollectionAsync(requestInfo RequestInformation, typeName string, responseHandler ResponseHandler) ([]interface{}, error)
	SendNoContentAsync(requestInfo RequestInformation, responseHandler ResponseHandler) error
	GetSerializationWriterFactory() s.SerializationWriterFactory
	EnableBackingStore()
}
