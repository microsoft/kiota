package nethttplibrary

import (
	"errors"
	nethttp "net/http"

	abs "github.com/microsoft/kiota/abstractions/go"
)

// ClientMiddleware is a middleware that wraps a net/http.Client
type ClientMiddleware struct {
	// client is the net/http.Client that will be used to make the request
	client *nethttp.Client
}

// NewClientMiddleware returns a new ClientMiddleware
// Parameters:
// 		client: the net/http.Client that will be used to make the request
// Returns:
// 		*ClientMiddleware: the new ClientMiddleware
func NewClientMiddleware(client *nethttp.Client) *ClientMiddleware {
	if client == nil {
		client = GetDefaultClient()
	}
	return &ClientMiddleware{client: client}
}
func (c *ClientMiddleware) Do(req *nethttp.Request, options []abs.RequestOption) (*nethttp.Response, error) {
	if c.client == nil {
		return nil, errors.New("client cannot be nil")
	}
	return (*c.client).Do(req)
}
func (c *ClientMiddleware) SetNext(next Middleware) {
}
func (c *ClientMiddleware) GetNext() Middleware {
	return nil
}
