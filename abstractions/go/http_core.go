package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

type HttpCore interface {
	SendAsync(requestInfo RequestInformation, constructor func() s.Parsable, responseHandler ResponseHandler) func() (s.Parsable, error)
	SendPrimitiveAsync(requestInfo RequestInformation, typeName string, responseHandler ResponseHandler) func() (interface{}, error)
	SendNoContentAsync(requestInfo RequestInformation, responseHandler ResponseHandler) func() error
	GetSerializationWriterFactory() (s.SerializationWriterFactory, error)
	EnableBackingStore()
}
