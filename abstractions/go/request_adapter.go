// Package abstractions provides the base infrastructure for the Kiota-generated SDKs to function.
// It defines multiple concepts related to abstract HTTP requests, serialization, and authentication.
// These concepts can then be implemented independently without tying the SDKs to any specific implementation.
// Kiota also provides default implementations for these concepts.
// Checkout:
// - github.com/microsoft/kiota/authentication/go/azure
// - github.com/microsoft/kiota/http/go/nethttp
// - github.com/microsoft/kiota/serialization/go/json
package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

// ErrorMappings is a mapping of status codes to error types factories.
type ErrorMappings map[string]s.ParsableFactory

// RequestAdapter is the service responsible for translating abstract RequestInformation into native HTTP requests.
type RequestAdapter interface {
	// SendAsync executes the HTTP request specified by the given RequestInformation and returns the deserialized response model.
	SendAsync(requestInfo RequestInformation, constructor s.ParsableFactory, responseHandler ResponseHandler, errorMappings ErrorMappings) (s.Parsable, error)
	// SendCollectionAsync executes the HTTP request specified by the given RequestInformation and returns the deserialized response model collection.
	SendCollectionAsync(requestInfo RequestInformation, constructor s.ParsableFactory, responseHandler ResponseHandler, errorMappings ErrorMappings) ([]s.Parsable, error)
	// SendPrimitiveAsync executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model.
	SendPrimitiveAsync(requestInfo RequestInformation, typeName string, responseHandler ResponseHandler, errorMappings ErrorMappings) (interface{}, error)
	// SendPrimitiveCollectionAsync executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model collection.
	SendPrimitiveCollectionAsync(requestInfo RequestInformation, typeName string, responseHandler ResponseHandler, errorMappings ErrorMappings) ([]interface{}, error)
	// SendNoContentAsync executes the HTTP request specified by the given RequestInformation with no return content.
	SendNoContentAsync(requestInfo RequestInformation, responseHandler ResponseHandler, errorMappings ErrorMappings) error
	// GetSerializationWriterFactory returns the serialization writer factory currently in use for the request adapter service.
	GetSerializationWriterFactory() s.SerializationWriterFactory
	// EnableBackingStore enables the backing store proxies for the SerializationWriters and ParseNodes in use.
	EnableBackingStore()
	// SetBaseUrl sets the base url for every request.
	SetBaseUrl(baseUrl string)
	// GetBaseUrl gets the base url for every request.
	GetBaseUrl() string
}
