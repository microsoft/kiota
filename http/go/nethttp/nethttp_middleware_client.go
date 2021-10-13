package nethttplibrary

import (
	"errors"
	nethttp "net/http"
)

type NetHttpMiddlewareClient struct {
	middlewares []Middleware
}

func NewNetHttpMiddlewareClient(middlewares []Middleware) (*NetHttpMiddlewareClient, error) {
	if len(middlewares) == 0 {
		middlewares = GetDefaultMiddlewares()
		err := ChainMiddlewares(middlewares)
		if err != nil {
			return nil, err
		}
	}
	return &NetHttpMiddlewareClient{}, nil
}

func (c *NetHttpMiddlewareClient) Do(req *nethttp.Request) (*nethttp.Response, error) {
	if c.middlewares == nil || len(c.middlewares) == 0 {
		return nil, errors.New("middlewares cannot be nil or empty")
	}
	return c.middlewares[0].Do(req)
}
