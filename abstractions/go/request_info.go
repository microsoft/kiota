package abstractions

import (
	"errors"

	u "net/url"

	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

/* This type represents an abstract HTTP request. */
type RequestInfo struct {
	Method          HttpMethod
	URI             u.URL
	Headers         map[string]string
	QueryParameters map[string]string
	Content         []byte
}

const contentTypeHeader = "Content-Type"
const binaryContentType = "application/octet-steam"

func SetStreamContent(request *RequestInfo, content []byte) {
	request.Content = content
	request.Headers[contentTypeHeader] = binaryContentType
}
func SetContentFromParsable(request *RequestInfo, coreService HttpCore, item s.Parsable, contentType string) error {
	if contentType == "" {
		return errors.New("content type cannot be empty")
	} else if coreService == nil {
		return errors.New("coreService cannot be nil")
	}
	factory, err := coreService.GetSerializationWriterFactory()
	if err != nil {
		return err
	} else if factory == nil {
		return errors.New("factory cannot be nil")
	}
	writer, err := factory.GetSerializationWriter(contentType)
	if err != nil {
		return err
	} else if writer == nil {
		return errors.New("writer cannot be nil")
	}
	defer writer.Close()
	writeErr := writer.WriterObjectValue("", item)
	if writeErr != nil {
		return writeErr
	}
	content, err := writer.GetSerializedContent()
	if err != nil {
		return err
	} else if content == nil {
		return errors.New("content cannot be nil")
	}
	request.Content = content
	request.Headers[contentTypeHeader] = contentType
	return nil
}
