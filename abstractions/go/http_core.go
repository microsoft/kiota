package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

type HttpCore interface {
	SendAsync(requestInfo *RequestInfo, responseHandler ResponseHandler) (s.Parsable, error)
	GetSerializationWriterFactory() (s.SerializationWriterFactory, error)
}
