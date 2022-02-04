// Package nethttplibrary implements the Kiota abstractions with net/http to execute the requests.
// It also provides a middleware infrastructure with some default middleware handlers like the retry handler and the redirect handler.
package nethttplibrary

import (
	nethttp "net/http"
)

// Create a new default net/http client with the options configured for the Kiota request adapter
func GetDefaultClient(middleware ...Middleware) *nethttp.Client {
	client := getDefaultClientWithoutMiddleware()
	client.Transport = NewCustomTransport(middleware...)
	return client
}

// used for internal unit testing
func getDefaultClientWithoutMiddleware() *nethttp.Client {
	client := nethttp.DefaultClient //TODO add default configuration and new up the client instead of using the default
	client.CheckRedirect = func(req *nethttp.Request, via []*nethttp.Request) error {
		return nethttp.ErrUseLastResponse
	}
	return client
}

// Creates a new default set of middlewares for the Kiota request adapter
func GetDefaultMiddlewares() []Middleware {
	return []Middleware{
		NewRetryHandler(),
		NewRedirectHandler(),
		&CompressionHandler{},
		//TODO add additional middlewares
	}
}
