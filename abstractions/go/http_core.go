package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

type HttpCore interface {
	SendAsync(requestInfo RequestInfo, constructor func() s.Parsable, responseHandler ResponseHandler) func() (s.Parsable, error)
	SendPrimitiveAsync(requestInfo RequestInfo, typeName string, responseHandler ResponseHandler) func() (interface{}, error)
	SendNoContentAsync(requestInfo RequestInfo, responseHandler ResponseHandler) func() error
	GetSerializationWriterFactory() (s.SerializationWriterFactory, error)
	EnableBackingStore()
}
