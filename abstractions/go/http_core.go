package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

type HttpCore interface {
	SendAsync(requestInfo RequestInfo, constructor func() s.Parsable, responseHandler ResponseHandler) func() (s.Parsable, error)
	SendNoContentAsync(requestInfo RequestInfo, responseHandler ResponseHandler) func() error
	GetSerializationWriterFactory() (s.SerializationWriterFactory, error)
}
