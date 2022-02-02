package abstractions

import (
	"errors"

	"reflect"
	"strconv"
	"strings"

	u "net/url"

	s "github.com/microsoft/kiota/abstractions/go/serialization"
	t "github.com/yosida95/uritemplate/v3"
)

// RequestInformation represents an abstract HTTP request.
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

// NewRequestInformation creates a new RequestInformation object with default values.
func NewRequestInformation() *RequestInformation {
	return &RequestInformation{
		Headers:         make(map[string]string),
		QueryParameters: make(map[string]string),
		options:         make(map[string]RequestOption),
		PathParameters:  make(map[string]string),
	}
}

// GetUri returns the URI of the request.
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
		varNames := uriTemplate.Varnames()
		normalizedNames := make(map[string]string)
		for _, varName := range varNames {
			normalizedNames[strings.ToLower(varName)] = varName
		}
		for key, value := range request.PathParameters {
			addParameterWithOriginalName(key, value, normalizedNames, values)
		}
		for key, value := range request.QueryParameters {
			addParameterWithOriginalName(key, value, normalizedNames, values)
		}
		url, err := uriTemplate.Expand(values)
		if err != nil {
			return nil, err
		}
		uri, err := u.Parse(url)
		return uri, err
	}
}

// addParameterWithOriginalName adds the URI template parameter to the template using the right casing, because of go conventions, casing might have changed for the generated property
func addParameterWithOriginalName(key string, value string, normalizedNames map[string]string, values t.Values) {
	lowercaseKey := strings.ToLower(key)
	if paramName, ok := normalizedNames[lowercaseKey]; ok {
		values.Set(paramName, t.String(value))
	} else {
		values.Set(key, t.String(value))
	}
}

// SetUri updates the URI for the request from a raw URL.
func (request *RequestInformation) SetUri(url u.URL) {
	request.uri = &url
	for k := range request.PathParameters {
		delete(request.PathParameters, k)
	}
	for k := range request.QueryParameters {
		delete(request.QueryParameters, k)
	}
}

// AddRequestOptions adds an option to the request to be read by the middleware infrastructure.
func (request *RequestInformation) AddRequestOptions(options ...RequestOption) error {
	if options == nil {
		return errors.New("RequestOptions cannot be nil")
	}
	if request.options == nil {
		request.options = make(map[string]RequestOption, len(options))
	}
	for _, option := range options {
		request.options[option.GetKey().Key] = option
	}
	return nil
}

// GetRequestOptions returns the options for this request. Options are unique by type. If an option of the same type is added twice, the last one wins.
func (request *RequestInformation) GetRequestOptions() []RequestOption {
	if request.options == nil {
		return []RequestOption{}
	}
	result := make([]RequestOption, len(request.options))
	idx := 0
	for _, option := range request.options {
		result[idx] = option
		idx++
	}
	return result
}

const contentTypeHeader = "Content-Type"
const binaryContentType = "application/octet-steam"

// SetStreamContent sets the request body to a binary stream.
func (request *RequestInformation) SetStreamContent(content []byte) {
	request.Content = content
	request.Headers[contentTypeHeader] = binaryContentType
}

// SetContentFromParsable sets the request body from a model with the specified content type.
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

// AddQueryParameters adds the query parameters to the request by reading the properties from the provided object.
func (request *RequestInformation) AddQueryParameters(source interface{}) {
	if source == nil || request == nil {
		return
	}
	valOfP := reflect.ValueOf(source)
	fields := reflect.TypeOf(source)
	numOfFields := fields.NumField()
	for i := 0; i < numOfFields; i++ {
		field := fields.Field(i)
		fieldName := field.Name
		fieldValue := valOfP.Field(i)
		str, ok := fieldValue.Interface().(*string)
		if ok && str != nil {
			request.QueryParameters[fieldName] = *str
		}
		bl, ok := fieldValue.Interface().(*bool)
		if ok && bl != nil {
			request.QueryParameters[fieldName] = strconv.FormatBool(*bl)
		}
		it, ok := fieldValue.Interface().(*int32)
		if ok && it != nil {
			request.QueryParameters[fieldName] = strconv.FormatInt(int64(*it), 10)
		}
		arr, ok := fieldValue.Interface().([]string)
		if ok && len(arr) > 0 {
			request.QueryParameters[fieldName] = strings.Join(arr, ",")
		}
	}
}
