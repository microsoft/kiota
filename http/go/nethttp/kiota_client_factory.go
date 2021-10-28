package nethttplibrary

import (
	nethttp "net/http"
)

// Create a new default net/http client with the options configured for the Kiota request adapter
// Returns:
// 		the client
func GetDefaultClient(middleware ...Middleware) *nethttp.Client {
	client := nethttp.DefaultClient //TODO add default configuration and new up the client instead of using the default
	client.Transport = NewCustomTransport(middleware...)
	return client
}

// Creates a new default set of middlewares for the Kiota request adapter
// Parameters:
// 		options - the options to use for the middlewares
// Returns:
// 		the middlewares
func GetDefaultMiddlewares() []Middleware {
	return []Middleware{
		NewRetryHandler(),
		//TODO add additional middlewares
	}
}
