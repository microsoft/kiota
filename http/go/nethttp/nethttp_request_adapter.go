package nethttplibrary

import (
	"bytes"
	"errors"
	"io/ioutil"
	"strings"

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
	httpClient *NetHttpMiddlewareClient
	// authenticationProvider is the provider used to authenticate requests
	authenticationProvider absauth.AuthenticationProvider
}

// NewNetHttpRequestAdapter creates a new NetHttpRequestAdapter with the given parameters
// Parameters:
// authenticationProvider: the provider used to authenticate requests
// Returns:
// a new NetHttpRequestAdapter
func NewNetHttpRequestAdapter(authenticationProvider absauth.AuthenticationProvider) (*NetHttpRequestAdapter, error) {
	return NewNetHttpRequestAdapterWithParseNodeFactory(authenticationProvider, nil)
}

// NewNetHttpRequestAdapterWithParseNodeFactory creates a new NetHttpRequestAdapter with the given parameters
// Parameters:
// authenticationProvider: the provider used to authenticate requests
// parseNodeFactory: the factory used to create parse nodes
// Returns:
// a new NetHttpRequestAdapter
func NewNetHttpRequestAdapterWithParseNodeFactory(authenticationProvider absauth.AuthenticationProvider, parseNodeFactory absser.ParseNodeFactory) (*NetHttpRequestAdapter, error) {
	return NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactory(authenticationProvider, parseNodeFactory, nil)
}

// NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactory creates a new NetHttpRequestAdapter with the given parameters
// Parameters:
// authenticationProvider: the provider used to authenticate requests
// parseNodeFactory: the factory used to create parse nodes
// serializationWriterFactory: the factory used to create serialization writers
// Returns:
// a new NetHttpRequestAdapter
func NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactory(authenticationProvider absauth.AuthenticationProvider, parseNodeFactory absser.ParseNodeFactory, serializationWriterFactory absser.SerializationWriterFactory) (*NetHttpRequestAdapter, error) {
	return NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactoryAndHttpClient(authenticationProvider, parseNodeFactory, serializationWriterFactory, nil)
}

// NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactoryAndHttpClient creates a new NetHttpRequestAdapter with the given parameters
// Parameters:
// authenticationProvider: the provider used to authenticate requests
// parseNodeFactory: the factory used to create parse nodes
// serializationWriterFactory: the factory used to create serialization writers
// httpClient: the client used to send requests
// Returns:
// a new NetHttpRequestAdapter
func NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactoryAndHttpClient(authenticationProvider absauth.AuthenticationProvider, parseNodeFactory absser.ParseNodeFactory, serializationWriterFactory absser.SerializationWriterFactory, httpClient *NetHttpMiddlewareClient) (*NetHttpRequestAdapter, error) {
	if authenticationProvider == nil {
		return nil, errors.New("authenticationProvider cannot be nil")
	}
	result := &NetHttpRequestAdapter{
		serializationWriterFactory: serializationWriterFactory,
		parseNodeFactory:           parseNodeFactory,
		httpClient:                 httpClient,
		authenticationProvider:     authenticationProvider,
	}
	if result.httpClient == nil {
		defaultClient, err := NewNetHttpMiddlewareClient()
		if err != nil {
			return nil, err
		}
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
func (a *NetHttpRequestAdapter) GetSerializationWriterFactory() absser.SerializationWriterFactory {
	return a.serializationWriterFactory
}
func (a *NetHttpRequestAdapter) EnableBackingStore() {
	//TODO implement when backing store is available for go
}
func (a *NetHttpRequestAdapter) getHttpResponseMessage(requestInfo abs.RequestInformation) (*nethttp.Response, error) {
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
	return request, nil
}
func (a *NetHttpRequestAdapter) SendAsync(requestInfo abs.RequestInformation, constructor func() absser.Parsable, responseHandler abs.ResponseHandler) (absser.Parsable, error) {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return nil, err
	}
	if responseHandler != nil {
		result, err := responseHandler.HandleResponse(response)
		if err != nil {
			return nil, err
		}
		return result.(absser.Parsable), nil
	} else if response != nil {
		body, err := ioutil.ReadAll(response.Body)
		if err != nil {
			return nil, err
		}
		parseNode, err := a.parseNodeFactory.GetRootParseNode(a.getResponsePrimaryContentType(response), body)
		if err != nil {
			return nil, err
		}
		result, err := parseNode.GetObjectValue(constructor)
		return result, err
	} else {
		return nil, errors.New("response is nil")
	}
}
func (a *NetHttpRequestAdapter) SendCollectionAsync(requestInfo abs.RequestInformation, constructor func() absser.Parsable, responseHandler abs.ResponseHandler) ([]absser.Parsable, error) {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return nil, err
	}
	if responseHandler != nil {
		result, err := responseHandler.HandleResponse(response)
		if err != nil {
			return nil, err
		}
		return result.([]absser.Parsable), nil
	} else if response != nil {
		body, err := ioutil.ReadAll(response.Body)
		if err != nil {
			return nil, err
		}
		parseNode, err := a.parseNodeFactory.GetRootParseNode(a.getResponsePrimaryContentType(response), body)
		if err != nil {
			return nil, err
		}
		result, err := parseNode.GetCollectionOfObjectValues(constructor)
		return result, err
	} else {
		return nil, errors.New("response is nil")
	}
}
func (a *NetHttpRequestAdapter) SendPrimitiveAsync(requestInfo abs.RequestInformation, typeName string, responseHandler abs.ResponseHandler) (interface{}, error) {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return nil, err
	}
	if responseHandler != nil {
		result, err := responseHandler.HandleResponse(response)
		if err != nil {
			return nil, err
		}
		return result.(absser.Parsable), nil
	} else if response != nil {
		body, err := ioutil.ReadAll(response.Body)
		if err != nil {
			return nil, err
		}
		parseNode, err := a.parseNodeFactory.GetRootParseNode(a.getResponsePrimaryContentType(response), body)
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
func (a *NetHttpRequestAdapter) SendPrimitiveCollectionAsync(requestInfo abs.RequestInformation, typeName string, responseHandler abs.ResponseHandler) ([]interface{}, error) {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return nil, err
	}
	if responseHandler != nil {
		result, err := responseHandler.HandleResponse(response)
		if err != nil {
			return nil, err
		}
		return result.([]interface{}), nil
	} else if response != nil {
		body, err := ioutil.ReadAll(response.Body)
		if err != nil {
			return nil, err
		}
		parseNode, err := a.parseNodeFactory.GetRootParseNode(a.getResponsePrimaryContentType(response), body)
		if err != nil {
			return nil, err
		}
		return parseNode.GetCollectionOfPrimitiveValues(typeName)
	} else {
		return nil, errors.New("response is nil")
	}
}
func (a *NetHttpRequestAdapter) SendNoContentAsync(requestInfo abs.RequestInformation, responseHandler abs.ResponseHandler) error {
	response, err := a.getHttpResponseMessage(requestInfo)
	if err != nil {
		return err
	}
	if responseHandler != nil {
		_, err := responseHandler.HandleResponse(response)
		return err
	} else if response != nil {
		return nil
	} else {
		return errors.New("response is nil")
	}
}
