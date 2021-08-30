package abstractions

import (
	"errors"
	"reflect"
	"strings"

	"net/url"
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
	options         map[string]MiddlewareOption
}

func NewRequestInfo() *RequestInfo {
	return &RequestInfo{
		URI:             u.URL{},
		Headers:         make(map[string]string),
		QueryParameters: make(map[string]string),
		options:         make(map[string]MiddlewareOption),
	}
}

func (request *RequestInfo) SetUri(currentPath string, pathSegment string, isRawUrl bool) error {
	if isRawUrl {
		if currentPath == "" {
			return errors.New("current path cannot be empty")
		}
		questionMarkSplat := strings.Split(currentPath, "?")
		schemeHostAndPath := questionMarkSplat[0]
		uri, err := url.Parse(schemeHostAndPath)
		if err != nil {
			return err
		}
		request.URI = *uri
		if len(questionMarkSplat) > 1 {
			queryParameters := questionMarkSplat[1]
			for _, queryParameter := range strings.Split(queryParameters, "&") {
				keyValue := strings.Split(queryParameter, "=")
				if len(keyValue) == 2 {
					request.QueryParameters[keyValue[0]] = keyValue[1]
				} else if len(keyValue) == 1 {
					request.QueryParameters[keyValue[0]] = ""
				}
			}
		}
	} else {
		uri, err := url.Parse(currentPath + pathSegment)
		if err != nil {
			return err
		}
		request.URI = *uri
	}
	return nil
}

func (request *RequestInfo) AddMiddlewareOptions(options ...MiddlewareOption) error {
	if options == nil {
		return errors.New("MiddlewareOptions cannot be nil")
	}
	if request.options == nil {
		request.options = make(map[string]MiddlewareOption, len(options))
	}
	for _, option := range options {
		tp := reflect.TypeOf(option)
		name := tp.Name()
		request.options[name] = option
	}
	return nil
}

func (request *RequestInfo) GetMiddlewareOptions() []MiddlewareOption {
	if request.options == nil {
		return []MiddlewareOption{}
	}
	result := make([]MiddlewareOption, len(request.options))
	for _, option := range request.options {
		result = append(result, option)
	}
	return result
}

const contentTypeHeader = "Content-Type"
const binaryContentType = "application/octet-steam"

func (request *RequestInfo) SetStreamContent(content []byte) {
	request.Content = content
	request.Headers[contentTypeHeader] = binaryContentType
}
func (request *RequestInfo) SetContentFromParsable(coreService HttpCore, contentType string, items ...s.Parsable) error {
	if contentType == "" {
		return errors.New("content type cannot be empty")
	} else if coreService == nil {
		return errors.New("coreService cannot be nil")
	} else if items == nil || len(items) == 0 {
		return errors.New("items cannot be nil or empty")
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
	var writeErr error
	if len(items) == 1 {
		writeErr = writer.WriteObjectValue("", items[0])
	} else {
		writeErr = writer.WriteCollectionOfObjectValues("", items)
	}
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
