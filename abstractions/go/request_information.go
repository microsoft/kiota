package abstractions

import (
	"errors"
	"reflect"

	u "net/url"

	s "github.com/microsoft/kiota/abstractions/go/serialization"
	t "github.com/yosida95/uritemplate/v3"
)

/* This type represents an abstract HTTP request. */
type RequestInformation struct {
	Method          HttpMethod
	uri             *u.URL
	Headers         map[string]string
	QueryParameters map[string]string
	Content         []byte
	PathParameters  map[string]string
	UrlTemplate     string
	options         map[string]RequestOption
}

const raw_url_key = "request-raw-url"

func NewRequestInformation() *RequestInformation {
	return &RequestInformation{
		Headers:         make(map[string]string),
		QueryParameters: make(map[string]string),
		options:         make(map[string]RequestOption),
		PathParameters:  make(map[string]string),
	}
}

func (request *RequestInformation) GetUri() (*u.URL, error) {
	if request.uri != nil {
		return request.uri, nil
	} else if request.UrlTemplate == "" {
		return nil, errors.New("uri cannot be empty")
	} else if request.PathParameters == nil {
		return nil, errors.New("uri template parameters cannot be nil")
	} else if request.QueryParameters == nil {
		return nil, errors.New("uri query parameters cannot be nil")
	} else if request.PathParameters[raw_url_key] != "" {
		uri, err := u.Parse(request.PathParameters[raw_url_key])
		if err != nil {
			return nil, err
		}
		err = request.SetUri(*uri)
		if err != nil {
			return nil, err
		}
		return request.uri, nil
	} else {
		uriTemplate, err := t.New(request.UrlTemplate)
		if err != nil {
			return nil, err
		}
		values := t.Values{}
		for key, value := range request.PathParameters {
			values.Set(key, t.String(value))
		}
		for key, value := range request.QueryParameters {
			values.Set(key, t.String(value))
		}
		url, err := uriTemplate.Expand(values)
		if err != nil {
			return nil, err
		}
		uri, err := u.Parse(url)
		return uri, err
	}
}

func (request *RequestInformation) SetUri(url u.URL) error {
	request.uri = &url
	for k := range request.PathParameters {
		delete(request.PathParameters, k)
	}
	for k := range request.QueryParameters {
		delete(request.QueryParameters, k)
	}
	return nil
}

func (request *RequestInformation) AddRequestOptions(options ...RequestOption) error {
	if options == nil {
		return errors.New("RequestOptions cannot be nil")
	}
	if request.options == nil {
		request.options = make(map[string]RequestOption, len(options))
	}
	for _, option := range options {
		tp := reflect.TypeOf(option)
		name := tp.Name()
		request.options[name] = option
	}
	return nil
}

func (request *RequestInformation) GetRequestOptions() []RequestOption {
	if request.options == nil {
		return []RequestOption{}
	}
	result := make([]RequestOption, len(request.options))
	for _, option := range request.options {
		result = append(result, option)
	}
	return result
}

const contentTypeHeader = "Content-Type"
const binaryContentType = "application/octet-steam"

func (request *RequestInformation) SetStreamContent(content []byte) {
	request.Content = content
	request.Headers[contentTypeHeader] = binaryContentType
}
func (request *RequestInformation) SetContentFromParsable(requestAdapter RequestAdapter, contentType string, items ...s.Parsable) error {
	if contentType == "" {
		return errors.New("content type cannot be empty")
	} else if requestAdapter == nil {
		return errors.New("requestAdapter cannot be nil")
	} else if len(items) == 0 {
		return errors.New("items cannot be nil or empty")
	}
	factory, err := requestAdapter.GetSerializationWriterFactory()
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
