package nethttplibrary

import (
	netthttp "net/http"
)

type Middleware interface {
	GetNext() Middleware
	SetNext(value Middleware)
	Do(req *netthttp.Request) (*netthttp.Response, error)
}
