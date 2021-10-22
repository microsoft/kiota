package middleware

import (
	"errors"
	nethttp "net/http"

	http "github.com/microsoft/kiota/http/go/nethttp"
)

type CallbackHandler struct {
	requestCallback  func(request *nethttp.Request) error
	responseCallback func(response *nethttp.Response) error
	next             http.Middleware
}

func NewTelemetryHandler(requestCallback func(*nethttp.Request) error, responseCallback func(*nethttp.Response) error) *CallbackHandler {
	return &CallbackHandler{
		requestCallback:  requestCallback,
		responseCallback: responseCallback,
	}
}

func (c *CallbackHandler) Do(req *nethttp.Request) (*nethttp.Response, error) {
	if c.next == nil {
		return nil, errors.New("telemetry handler: the next middleware is nil")
	}
	if c.requestCallback != nil {
		err := c.requestCallback(req)
		if err != nil {
			return nil, err
		}
	}
	response, err := c.next.Do(req)
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
