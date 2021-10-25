package abstractions

import (
	s "github.com/microsoft/kiota/abstractions/go/serialization"
)

// Service responsible for translating abstract Request Info into concrete native HTTP requests.
type RequestAdapter interface {
	// Executes the HTTP request specified by the given RequestInformation and returns the deserialized response model.
	// Parameters:
	//  - requestInfo: The RequestInformation object to use for the HTTP request.
	//  - constuctor: The factory for the result Parsable object
	//  - responseHandler: The response handler to use for the HTTP request instead of the default handler.
	// Returns:
	//  - The deserialized response model.
	//  - An error if any.
	SendAsync(requestInfo RequestInformation, constructor func() s.Parsable, responseHandler ResponseHandler) (s.Parsable, error)
	// Executes the HTTP request specified by the given RequestInformation and returns the deserialized response model collection.
	// Parameters:
	//  - requestInfo: The RequestInformation object to use for the HTTP request.
	//  - constuctor: The factory for the result Parsable object
	//  - responseHandler: The response handler to use for the HTTP request instead of the default handler.
	// Returns:
	//  - The deserialized response model collection.
	//  - An error if any.
	SendCollectionAsync(requestInfo RequestInformation, constructor func() s.Parsable, responseHandler ResponseHandler) ([]s.Parsable, error)
	// Executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model.
	// Parameters:
	//  - requestInfo: The RequestInformation object to use for the HTTP request.
	//  - typeName: The type name of the response model.
	//  - responseHandler: The response handler to use for the HTTP request instead of the default handler.
	// Returns:
	//  - The deserialized response model.
	//  - An error if any.
	SendPrimitiveAsync(requestInfo RequestInformation, typeName string, responseHandler ResponseHandler) (interface{}, error)
	// Executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model collection.
	// Parameters:
	//  - requestInfo: The RequestInformation object to use for the HTTP request.
	//  - typeName: The type name of the response model.
	//  - responseHandler: The response handler to use for the HTTP request instead of the default handler.
	// Returns:
	//  - The deserialized response model collection.
	//  - An error if any.
	SendPrimitiveCollectionAsync(requestInfo RequestInformation, typeName string, responseHandler ResponseHandler) ([]interface{}, error)
	// Executes the HTTP request specified by the given RequestInformation with no return content.
	// Parameters:
	//  - requestInfo: The RequestInformation object to use for the HTTP request.
	//  - responseHandler: The response handler to use for the HTTP request instead of the default handler.
	// Returns:
	//  - An error if any.
	SendNoContentAsync(requestInfo RequestInformation, responseHandler ResponseHandler) error
	// Gets the serialization writer factory currently in use for the request adapter service.
	// Returns:
	//  - The serialization writer factory currently in use for the request adapter service.
	GetSerializationWriterFactory() s.SerializationWriterFactory
	// Enables the backing store proxies for the SerializationWriters and ParseNodes in use.
	EnableBackingStore()
}
