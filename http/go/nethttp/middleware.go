package nethttplibrary

import (
	netthttp "net/http"
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
	// Returns:
	//  - The response from the next middleware in the chain.
	//  - The error if any.
	Do(req *netthttp.Request) (*netthttp.Response, error)
}
