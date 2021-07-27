package serialization

import (
	i "io"
)

type SerializationWriter interface {
	i.Closer
	WriterObjectValue(key string, item Parsable) error
	//TODO all the different data types
	GetSerializedContent() ([]byte, error)
}
