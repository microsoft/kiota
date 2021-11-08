package nethttplibrary

import nethttp "net/http"

// Pipeline contract for middleware infrastructure
type Pipeline interface {
	// Next moves the request object through middlewares in the pipeline
	// Parameters:
	//     req: the request object
	// Returns:
	//     the response object
	//     error: any error that occurred
	Next(req *nethttp.Request) (*nethttp.Response, error)
}

// custom transport for net/http with a middleware pipeline
type customTransport struct {
	nethttp.Transport
	// middleware pipeline in use for the client
	middlewarePipeline *middlewarePipeline
}

// middleware pipeline implementation using a roundtripper from net/http
type middlewarePipeline struct {
	// index of the middleware beeing executed
	middlewareIndex int
	// the round tripper to use to execute the request
	transport nethttp.RoundTripper
	// the middlewares to execute
	middlewares []Middleware
}

func newMiddlewarePipeline(middlewares []Middleware) *middlewarePipeline {
	return &middlewarePipeline{
		middlewareIndex: 0,
		transport:       nethttp.DefaultTransport,
		middlewares:     middlewares,
	}
}

func (pipeline *middlewarePipeline) incrementMiddlewareIndex() {
	pipeline.middlewareIndex++
}

// Next moves the request object through middlewares in the pipeline
func (pipeline *middlewarePipeline) Next(req *nethttp.Request) (*nethttp.Response, error) {
	if pipeline.middlewareIndex < len(pipeline.middlewares) {
		middleware := pipeline.middlewares[pipeline.middlewareIndex]

		pipeline.incrementMiddlewareIndex()
		return middleware.Intercept(pipeline, req)
	}

	return pipeline.transport.RoundTrip(req)
}

func (transport *customTransport) RoundTrip(req *nethttp.Request) (*nethttp.Response, error) {
	return transport.middlewarePipeline.Next(req)
}

// Creates a new custom transport for http client with the provided set of middleware
// Parameters:
//     middlewares: the middlewares to use
// Returns:
//     the custom transport
func NewCustomTransport(middlewares ...Middleware) *customTransport {
	if len(middlewares) == 0 {
		middlewares = GetDefaultMiddlewares()
	}
	return &customTransport{
		middlewarePipeline: newMiddlewarePipeline(middlewares),
	}
}
