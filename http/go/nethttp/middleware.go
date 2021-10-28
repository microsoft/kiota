package nethttplibrary

import nethttp "net/http"

// Middleware interface for cross cutting concerns with HTTP requests and responses.
type Middleware interface {
	// intercepts the request and returns the resposne. The implementer MUST call pipeline.Next()
	// Parameters:
	// 		- the pipeline to be executed after the middleware
	// 		- the request to be processed
	// Returns:
	// 		- the response
	// 		- error if any
	Intercept(Pipeline, *nethttp.Request) (*nethttp.Response, error)
}
