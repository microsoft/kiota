package abstractions

import (
	"errors"
	"reflect"

	u "net/url"

	s "github.com/microsoft/kiota/abstractions/go/serialization"
	t "github.com/yosida95/uritemplate/v3"
)

// This type represents an abstract HTTP request.
type RequestInformation struct {
	// The HTTP method of the request.
	Method HttpMethod
	uri    *u.URL
	// The Request Headers.
	Headers map[string]string
	// The Query Parameters of the request.
	QueryParameters map[string]string
	// The Request Body.
	Content []byte
	// The path parameters to use for the URL template when generating the URI.
	PathParameters map[string]string
	// The Url template for the current request.
	UrlTemplate string
	options     map[string]RequestOption
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

// Get the URI of the request.
// Returns:
// 		- The URI of the request.
// 		- An error if the URI cannot be retrieved.
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
		request.SetUri(*uri)
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

// Sets the URI for the request from a raw URL.
// Parameters:
// 		- url: The raw URL to set the URI to.
func (request *RequestInformation) SetUri(url u.URL) {
	request.uri = &url
	for k := range request.PathParameters {
		delete(request.PathParameters, k)
	}
	for k := range request.QueryParameters {
		delete(request.QueryParameters, k)
	}
}

// Adds an option to the request.
// Parameters:
// 		- option: The option to add to the request.
// Returns:
// 		- An error if the option cannot be added.
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

// Gets the options for this request. Options are unique by type. If an option of the same type is added twice, the last one wins.
// Returns:
// 		- The options for this request.
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

// Sets the request body to a binary stream.
// Parameters:
// 		- content: The binary stream to set the request body to.
func (request *RequestInformation) SetStreamContent(content []byte) {
	request.Content = content
	request.Headers[contentTypeHeader] = binaryContentType
}

// Sets the request body from a model with the specified content type.
// Parameters:
// 		- requestAdapter: The request adapter to use to get the request body from the model.
//      - contentType: The content type to set the request body to.
//      - item: The model to set the request body from.
// Returns:
// 		- An error if the request body cannot be set.
func (request *RequestInformation) SetContentFromParsable(requestAdapter RequestAdapter, contentType string, items ...s.Parsable) error {
	if contentType == "" {
		return errors.New("content type cannot be empty")
	} else if requestAdapter == nil {
		return errors.New("requestAdapter cannot be nil")
	} else if len(items) == 0 {
		return errors.New("items cannot be nil or empty")
	}
	factory := requestAdapter.GetSerializationWriterFactory()
	if factory == nil {
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
