package middleware

import (
	"errors"
	nethttp "net/http"

	abs "github.com/microsoft/kiota/abstractions/go"
	http "github.com/microsoft/kiota/http/go/nethttp"
)

// CallbackHandler is a middleware that calls callback functions before and after executing the next middleware.
type CallbackHandler struct {
	// Callback to execute before the next middleware.
	requestCallback func(request *nethttp.Request) error
	// Callback to execute after the next middleware.
	responseCallback func(response *nethttp.Response) error
	// The next middleware.
	next http.Middleware
}

// NewCallbackHandler creates a new instance of CallbackHandler.
// Parameters:
// 		requestCallback: the callback to execute before the next middleware.
// 		responseCallback: the callback to execute after the next middleware.
// Returns:
// 		a new instance of CallbackHandler.
func NewCallbackHandler(requestCallback func(*nethttp.Request) error, responseCallback func(*nethttp.Response) error) *CallbackHandler {
	return &CallbackHandler{
		requestCallback:  requestCallback,
		responseCallback: responseCallback,
	}
}

func (c *CallbackHandler) Do(req *nethttp.Request, options []abs.RequestOption) (*nethttp.Response, error) {
	if c.next == nil {
		return nil, errors.New("callback handler: the next middleware is nil")
	}
	if c.requestCallback != nil {
		err := c.requestCallback(req)
		if err != nil {
			return nil, err
		}
	}
	response, err := c.next.Do(req, options)
	if err != nil {
		return nil, err
	}
	if c.responseCallback != nil {
		err := c.responseCallback(response)
		if err != nil {
			return nil, err
		}
	}
	return response, nil
}

func (c *CallbackHandler) SetNext(next http.Middleware) {
	c.next = next
}
func (c *CallbackHandler) GetNext() http.Middleware {
	return c.next
}
