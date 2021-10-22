package nethttplibrary

import (
	"errors"
	nethttp "net/http"
	"reflect"
)

// Create a new default net/http client with the options configured for the Kiota request adapter
// Returns:
// 		the client
func GetDefaultClient() *nethttp.Client {
	return nethttp.DefaultClient //TODO add default configuration and new up the client instead of using the default
}

// Creates a new default set of middlewares for the Kiota request adapter
// Parameters:
// 		options - the options to use for the middlewares
// Returns:
// 		the middlewares
func GetDefaultMiddlewares() []Middleware {
	return []Middleware{
		NewClientMiddleware(GetDefaultClient()), //TODO add additional middlewares
	}
}

// Chains the middlewares together, the last middleware MUST perform the request and return the response
// Parameters:
// 		middlewares - the middlewares to chain
func ChainMiddlewares(middlewares []Middleware) error {
	arrayLength := len(middlewares)
	if arrayLength == 0 {
		return errors.New("middlewares cannot be nil or empty")
	}
	for i := 0; i < arrayLength; i++ {
		if i+1 < arrayLength {
			middlewares[i].SetNext(middlewares[i+1])
		} else {
			middlewares[i].SetNext(nil)
		}
	}
	if reflect.TypeOf(middlewares[arrayLength-1]) != reflect.TypeOf(NewClientMiddleware(nil)) {
		return errors.New("last middleware must be a client middleware")
	}
	return nil
}
