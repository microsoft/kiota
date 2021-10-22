package nethttplibrary

import (
	"errors"
	nethttp "net/http"
)

// Http client with middleware infrastructure to use by the request adapter service.
// The last middleware in the chain MUST execute the request and return the response.
// See the ClientMiddleware for that last middleware.
type NetHttpMiddlewareClient struct {
	middlewares []Middleware
}

// Creates a new NetHttpMiddlewareClient with the default middlewares.
// Returns:
// 		the new NetHttpMiddlewareClient.
func NewNetHttpMiddlewareClient() (*NetHttpMiddlewareClient, error) {
	return NewNetHttpMiddlewareClientWithMiddlewares(nil)
}

// Creates a new NetHttpMiddlewareClient with the given middlewares.
// Parameters:
// 		middlewares: the middlewares to be used by the client.
// Returns:
// 		the new NetHttpMiddlewareClient.
func NewNetHttpMiddlewareClientWithMiddlewares(middlewares []Middleware) (*NetHttpMiddlewareClient, error) {
	if len(middlewares) == 0 {
		middlewares = GetDefaultMiddlewares()
		err := ChainMiddlewares(middlewares)
		if err != nil {
			return nil, err
		}
	}
	return &NetHttpMiddlewareClient{
		middlewares: middlewares,
	}, nil
}

// Executes the given request with the current middlewares.
// Parameters:
// 		req: the request to be executed.
// Returns:
// 		the response of the request.
func (c *NetHttpMiddlewareClient) Do(req *nethttp.Request) (*nethttp.Response, error) {
	if c.middlewares == nil || len(c.middlewares) == 0 {
		return nil, errors.New("middlewares cannot be nil or empty")
	}
	return c.middlewares[0].Do(req)
}
