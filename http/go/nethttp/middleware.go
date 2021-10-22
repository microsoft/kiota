package nethttplibrary

import (
	netthttp "net/http"

	abs "github.com/microsoft/kiota/abstractions/go"
)

// Middleware interface for cross cutting concerns with HTTP requests and responses.
type Middleware interface {
	// Gets the next middleware in the chain.
	GetNext() Middleware
	// Sets the next middleware in the chain.
	SetNext(value Middleware)
	// Processes the request.
	// Parameters:
	//  - request: The request to process.
	//  - options: The options to use for the middlewares.
	// Returns:
	//  - The response from the next middleware in the chain.
	//  - The error if any.
	Do(req *netthttp.Request, options []abs.RequestOption) (*netthttp.Response, error)
}
