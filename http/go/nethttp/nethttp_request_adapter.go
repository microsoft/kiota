package nethttplibrary

import (
	"bytes"
	"errors"
	"io/ioutil"
	"strconv"
	"strings"

	ctx "context"
	nethttp "net/http"

	abs "github.com/microsoft/kiota/abstractions/go"
	absauth "github.com/microsoft/kiota/abstractions/go/authentication"
	absser "github.com/microsoft/kiota/abstractions/go/serialization"
)

// NetHttpRequestAdapter implements the RequestAdapter interface using net/http
type NetHttpRequestAdapter struct {
	// serializationWriterFactory is the factory used to create serialization writers
	serializationWriterFactory absser.SerializationWriterFactory
	// parseNodeFactory is the factory used to create parse nodes
	parseNodeFactory absser.ParseNodeFactory
	// httpClient is the client used to send requests
	httpClient *nethttp.Client
	// authenticationProvider is the provider used to authenticate requests
	authenticationProvider absauth.AuthenticationProvider
	// The base url for every request.
	baseUrl string
}

// NewNetHttpRequestAdapter creates a new NetHttpRequestAdapter with the given parameters
func NewNetHttpRequestAdapter(authenticationProvider absauth.AuthenticationProvider) (*NetHttpRequestAdapter, error) {
	return NewNetHttpRequestAdapterWithParseNodeFactory(authenticationProvider, nil)
}

// NewNetHttpRequestAdapterWithParseNodeFactory creates a new NetHttpRequestAdapter with the given parameters
func NewNetHttpRequestAdapterWithParseNodeFactory(authenticationProvider absauth.AuthenticationProvider, parseNodeFactory absser.ParseNodeFactory) (*NetHttpRequestAdapter, error) {
	return NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactory(authenticationProvider, parseNodeFactory, nil)
}

// NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactory creates a new NetHttpRequestAdapter with the given parameters
func NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactory(authenticationProvider absauth.AuthenticationProvider, parseNodeFactory absser.ParseNodeFactory, serializationWriterFactory absser.SerializationWriterFactory) (*NetHttpRequestAdapter, error) {
	return NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactoryAndHttpClient(authenticationProvider, parseNodeFactory, serializationWriterFactory, nil)
}

// NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactoryAndHttpClient creates a new NetHttpRequestAdapter with the given parameters
func NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactoryAndHttpClient(authenticationProvider absauth.AuthenticationProvider, parseNodeFactory absser.ParseNodeFactory, serializationWriterFactory absser.SerializationWriterFactory, httpClient *nethttp.Client) (*NetHttpRequestAdapter, error) {
	if authenticationProvider == nil {
		return nil, errors.New("authenticationProvider cannot be nil")
	}
	result := &NetHttpRequestAdapter{
		serializationWriterFactory: serializationWriterFactory,
		parseNodeFactory:           parseNodeFactory,
		httpClient:                 httpClient,
		authenticationProvider:     authenticationProvider,
		baseUrl:                    "",
	}
	if result.httpClient == nil {
		defaultClient := GetDefaultClient()
		result.httpClient = defaultClient
	}
	if result.serializationWriterFactory == nil {
		result.serializationWriterFactory = absser.DefaultSerializationWriterFactoryInstance
	}
	if result.parseNodeFactory == nil {
		result.parseNodeFactory = absser.DefaultParseNodeFactoryInstance
	}
	return result, nil
}

// GetSerializationWriterFactory returns the serialization writer factory currently in use for the request adapter service.
func (a *NetHttpRequestAdapter) GetSerializationWriterFactory() absser.SerializationWriterFactory {
	return a.serializationWriterFactory
}

// EnableBackingStore enables the backing store proxies for the SerializationWriters and ParseNodes in use.
func (a *NetHttpRequestAdapter) EnableBackingStore() {
	//TODO implement when backing store is available for go
}

// SetBaseUrl sets the base url for every request.
func (a *NetHttpRequestAdapter) SetBaseUrl(baseUrl string) {
	a.baseUrl = baseUrl
}

// GetBaseUrl gets the base url for every request.
func (a *NetHttpRequestAdapter) GetBaseUrl() string {
	return a.baseUrl
}
func (a *NetHttpRequestAdapter) getHttpResponseMessage(requestInfo abs.RequestInformation) (*nethttp.Response, error) {
	a.setBaseUrlForRequestInformation(requestInfo)
	err := a.authenticationProvider.AuthenticateRequest(requestInfo)
	if err != nil {
		return nil, err
	}
	request, err := a.getRequestFromRequestInformation(requestInfo)
	if err != nil {
		return nil, err
	}
	return (*a.httpClient).Do(request)
}
func (a *NetHttpRequestAdapter) getResponsePrimaryContentType(response *nethttp.Response) string {
	if response.Header == nil {
		return ""
	}
	rawType := response.Header.Get("Content-Type")
	splat := strings.Split(rawType, ";")
	return strings.ToLower(splat[0])
}
func (a *NetHttpRequestAdapter) setBaseUrlForRequestInformation(requestInfo abs.RequestInformation) {
	requestInfo.PathParameters["baseurl"] = a.GetBaseUrl()
}
func (a *NetHttpRequestAdapter) getRequestFromRequestInformation(requestInfo abs.RequestInformation) (*nethttp.Request, error) {
	uri, err := requestInfo.GetUri()
	if err != nil {
		return nil, err
	}
	request, err := nethttp.NewRequest(requestInfo.Method.String(), uri.String(), nil)
	if err != nil {
		return nil, err
	}
	if len(requestInfo.Content) > 0 {
		reader := bytes.NewReader(requestInfo.Content)
		request.Body = ioutil.NopCloser(reader)
	}
	if request.Header == nil {
		request.Header = make(nethttp.Header)
	}
	if requestInfo.Headers != nil {
		for key, value := range requestInfo.Headers {
			request.Header.Set(key, value)
		}
	}
	for _, value := range requestInfo.GetRequestOptions() {
		request = request.WithContext(ctx.WithValue(request.Context(), value.GetKey(), value))
	}
	return request, nil
}

// SendAsync executes the HTTP request specified by the given RequestInformation and returns the deserialized response model.
func (a *NetHttpRequestAdapter) SendAsync(requestInfo abs.RequestInformation, constructor absser.ParsableFactory, responseHandler abs.ResponseHandler, errorMappings abs.ErrorMappings) (absser.Parsable, error) {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return nil, err
	}
	if responseHandler != nil {
		result, err := responseHandler(response, errorMappings)
		if err != nil {
			return nil, err
		}
		return result.(absser.Parsable), nil
	} else if response != nil {
		err = a.throwFailedResponses(response, errorMappings)
		if err != nil {
			return nil, err
		}
		parseNode, err := a.getRootParseNode(response)
		if err != nil {
			return nil, err
		}
		result, err := parseNode.GetObjectValue(constructor)
		return result, err
	} else {
		return nil, errors.New("response is nil")
	}
}

// SendCollectionAsync executes the HTTP request specified by the given RequestInformation and returns the deserialized response model collection.
func (a *NetHttpRequestAdapter) SendCollectionAsync(requestInfo abs.RequestInformation, constructor absser.ParsableFactory, responseHandler abs.ResponseHandler, errorMappings abs.ErrorMappings) ([]absser.Parsable, error) {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return nil, err
	}
	if responseHandler != nil {
		result, err := responseHandler(response, errorMappings)
		if err != nil {
			return nil, err
		}
		return result.([]absser.Parsable), nil
	} else if response != nil {
		err = a.throwFailedResponses(response, errorMappings)
		if err != nil {
			return nil, err
		}
		parseNode, err := a.getRootParseNode(response)
		if err != nil {
			return nil, err
		}
		result, err := parseNode.GetCollectionOfObjectValues(constructor)
		return result, err
	} else {
		return nil, errors.New("response is nil")
	}
}

// SendPrimitiveAsync executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model.
func (a *NetHttpRequestAdapter) SendPrimitiveAsync(requestInfo abs.RequestInformation, typeName string, responseHandler abs.ResponseHandler, errorMappings abs.ErrorMappings) (interface{}, error) {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return nil, err
	}
	if responseHandler != nil {
		result, err := responseHandler(response, errorMappings)
		if err != nil {
			return nil, err
		}
		return result.(absser.Parsable), nil
	} else if response != nil {
		err = a.throwFailedResponses(response, errorMappings)
		if err != nil {
			return nil, err
		}
		if typeName == "[]byte" {
			return ioutil.ReadAll(response.Body)
		}
		parseNode, err := a.getRootParseNode(response)
		if err != nil {
			return nil, err
		}
		switch typeName {
		case "string":
			return parseNode.GetStringValue()
		case "float32":
			return parseNode.GetFloat32Value()
		case "float64":
			return parseNode.GetFloat64Value()
		case "int32":
			return parseNode.GetInt32Value()
		case "int64":
			return parseNode.GetInt64Value()
		case "bool":
			return parseNode.GetBoolValue()
		case "Time":
			return parseNode.GetTimeValue()
		case "UUID":
			return parseNode.GetUUIDValue()
		default:
			return nil, errors.New("unsupported type")
		}
	} else {
		return nil, errors.New("response is nil")
	}
}

// SendPrimitiveCollectionAsync executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model collection.
func (a *NetHttpRequestAdapter) SendPrimitiveCollectionAsync(requestInfo abs.RequestInformation, typeName string, responseHandler abs.ResponseHandler, errorMappings abs.ErrorMappings) ([]interface{}, error) {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return nil, err
	}
	if responseHandler != nil {
		result, err := responseHandler(response, errorMappings)
		if err != nil {
			return nil, err
		}
		return result.([]interface{}), nil
	} else if response != nil {
		err = a.throwFailedResponses(response, errorMappings)
		if err != nil {
			return nil, err
		}
		parseNode, err := a.getRootParseNode(response)
		if err != nil {
			return nil, err
		}
		return parseNode.GetCollectionOfPrimitiveValues(typeName)
	} else {
		return nil, errors.New("response is nil")
	}
}

// SendNoContentAsync executes the HTTP request specified by the given RequestInformation with no return content.
func (a *NetHttpRequestAdapter) SendNoContentAsync(requestInfo abs.RequestInformation, responseHandler abs.ResponseHandler, errorMappings abs.ErrorMappings) error {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return err
	}
	if responseHandler != nil {
		_, err := responseHandler(response, errorMappings)
		return err
	} else if response != nil {
		return nil
	} else {
		return errors.New("response is nil")
	}
}

func (a *NetHttpRequestAdapter) getRootParseNode(response *nethttp.Response) (absser.ParseNode, error) {
	body, err := ioutil.ReadAll(response.Body)
	if err != nil {
		return nil, err
	}
	return a.parseNodeFactory.GetRootParseNode(a.getResponsePrimaryContentType(response), body)
}

func (a *NetHttpRequestAdapter) throwFailedResponses(response *nethttp.Response, errorMappings abs.ErrorMappings) error {
	if response.StatusCode < 400 {
		return nil
	}

	statusAsString := strconv.Itoa(response.StatusCode)
	var errorCtor absser.ParsableFactory = nil
	if len(errorMappings) != 0 {
		if errorMappings[statusAsString] != nil {
			errorCtor = errorMappings[statusAsString]
		} else if response.StatusCode >= 400 && response.StatusCode < 500 && errorMappings["4XX"] != nil {
			errorCtor = errorMappings["4XX"]
		} else if response.StatusCode >= 500 && response.StatusCode < 600 && errorMappings["5XX"] != nil {
			errorCtor = errorMappings["5XX"]
		}
	}

	if errorCtor == nil {
		return &abs.ApiError{
			Message: "The server returned an unexpected status code and no error factory is registered for this code: " + statusAsString,
		}
	}

	rootNode, err := a.getRootParseNode(response)
	if err != nil {
		return err
	}

	errValue, err := rootNode.GetObjectValue(errorCtor)
	if err != nil {
		return err
	}

	return errValue.(error)
}
