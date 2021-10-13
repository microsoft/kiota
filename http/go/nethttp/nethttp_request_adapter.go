package nethttplibrary

import (
	"errors"
	"io/ioutil"

	nethttp "net/http"

	abs "github.com/microsoft/kiota/abstractions/go"
	absauth "github.com/microsoft/kiota/abstractions/go/authentication"
	absser "github.com/microsoft/kiota/abstractions/go/serialization"
)

type NetHttpRequestAdapter struct {
	serializationWriterFactory absser.SerializationWriterFactory
	parseNodeFactory           absser.ParseNodeFactory
	httpClient                 *NetHttpMiddlewareClient
	authenticationProvider     absauth.AuthenticationProvider
}

func NewNetHttpRequestAdapter(authenticationProvider absauth.AuthenticationProvider) (*NetHttpRequestAdapter, error) {
	return NewNetHttpRequestAdapterWithParseNodeFactory(authenticationProvider, nil)
}
func NewNetHttpRequestAdapterWithParseNodeFactory(authenticationProvider absauth.AuthenticationProvider, parseNodeFactory absser.ParseNodeFactory) (*NetHttpRequestAdapter, error) {
	return NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactory(authenticationProvider, parseNodeFactory, nil)
}
func NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactory(authenticationProvider absauth.AuthenticationProvider, parseNodeFactory absser.ParseNodeFactory, serializationWriterFactory absser.SerializationWriterFactory) (*NetHttpRequestAdapter, error) {
	return NewNetHttpRequestAdapterWithParseNodeFactoryAndSerializationWriterFactoryAndHttpClient(authenticationProvider, parseNodeFactory, serializationWriterFactory, nil)
}
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
		defaultClient, err := NewNetHttpMiddlewareClient(nil)
		if err != nil {
			return nil, err
		}
		result.httpClient = defaultClient
	}
	//TODO get parse node and serialization writers from factories singleton if nil
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
func (a *NetHttpRequestAdapter) getRequestFromRequestInformation(requestInfo abs.RequestInformation) (*nethttp.Request, error) {
	uri, err := requestInfo.GetUri()
	if err != nil {
		return nil, err
	}
	//TODO body 3rd argument
	request, err := nethttp.NewRequest(requestInfo.Method.String(), uri.String(), nil)
	if err != nil {
		return nil, err
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
		parseNode, err := a.parseNodeFactory.GetRootParseNode(response.Header.Get("Content-Type"), body)
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
		parseNode, err := a.parseNodeFactory.GetRootParseNode(response.Header.Get("Content-Type"), body)
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
		parseNode, err := a.parseNodeFactory.GetRootParseNode(response.Header.Get("Content-Type"), body)
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
		parseNode, err := a.parseNodeFactory.GetRootParseNode(response.Header.Get("Content-Type"), body)
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
