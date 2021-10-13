package nethttplibrary

import (
	"errors"
	nethttp "net/http"
)

type ClientMiddleware struct {
	client *nethttp.Client
}

func NewClientMiddleware(client *nethttp.Client) *ClientMiddleware {
	if client == nil {
		client = GetDefaultClient()
	}
	return &ClientMiddleware{client: client}
}
func (c *ClientMiddleware) Do(req *nethttp.Request) (*nethttp.Response, error) {
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
