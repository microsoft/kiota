package nethttplibrary

import nethttp "net/http"

type Pipeline interface {
	Next(req *nethttp.Request) (*nethttp.Response, error)
}
type customTransport struct {
	nethttp.Transport
	middlewarePipeline *middlewarePipeline
}

type middlewarePipeline struct {
	middlewareIndex int
	transport       nethttp.RoundTripper
	middlewares     []Middleware
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

func NewCustomTransport(middlewares ...Middleware) *customTransport {
	if len(middlewares) == 0 {
		middlewares = GetDefaultMiddlewares()
	}
	return &customTransport{
		middlewarePipeline: newMiddlewarePipeline(middlewares),
	}
}
