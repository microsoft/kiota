package nethttplibrary

import nethttp "net/http"

// Pipeline contract for middleware infrastructure
type Pipeline interface {
	// Next moves the request object through middlewares in the pipeline
	Next(req *nethttp.Request, middlewareIndex int) (*nethttp.Response, error)
}

// custom transport for net/http with a middleware pipeline
type customTransport struct {
	nethttp.Transport
	// middleware pipeline in use for the client
	middlewarePipeline *middlewarePipeline
}

// middleware pipeline implementation using a roundtripper from net/http
type middlewarePipeline struct {
	// the round tripper to use to execute the request
	transport nethttp.RoundTripper
	// the middlewares to execute
	middlewares []Middleware
}

func newMiddlewarePipeline(middlewares []Middleware) *middlewarePipeline {
	return &middlewarePipeline{
		transport:   nethttp.DefaultTransport,
		middlewares: middlewares,
	}
}

// Next moves the request object through middlewares in the pipeline
func (pipeline *middlewarePipeline) Next(req *nethttp.Request, middlewareIndex int) (*nethttp.Response, error) {
	if middlewareIndex < len(pipeline.middlewares) {
		middleware := pipeline.middlewares[middlewareIndex]
		return middleware.Intercept(pipeline, middlewareIndex+1, req)
	}

	return pipeline.transport.RoundTrip(req)
}

// RoundTrip executes the the next middleware and returns a response
func (transport *customTransport) RoundTrip(req *nethttp.Request) (*nethttp.Response, error) {
	return transport.middlewarePipeline.Next(req, 0)
}

// NewCustomTransport creates a new custom transport for http client with the provided set of middleware
func NewCustomTransport(middlewares ...Middleware) *customTransport {
	if len(middlewares) == 0 {
		middlewares = GetDefaultMiddlewares()
	}
	return &customTransport{
		middlewarePipeline: newMiddlewarePipeline(middlewares),
	}
}
