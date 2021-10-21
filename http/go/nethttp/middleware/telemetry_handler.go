package middleware

import (
	"errors"
	nethttp "net/http"

	http "github.com/microsoft/kiota/http/go/nethttp"
)

type TelemetryHandler struct {
	callback func(request *nethttp.Request) error
	next     http.Middleware
}

func NewTelemetryHandler(callback func(request *nethttp.Request) error) *TelemetryHandler {
	return &TelemetryHandler{
		callback: callback,
	}
}

func (c *TelemetryHandler) Do(req *nethttp.Request) (*nethttp.Response, error) {
	if c.callback == nil {
		return nil, errors.New("telemetry handler: the callback is nil")
	}
	if c.next == nil {
		return nil, errors.New("telemetry handler: the next middleware is nil")
	}
	err := c.callback(req)
	if err != nil {
		return nil, err
	}
	return c.next.Do(req)
}

func (c *TelemetryHandler) SetNext(next http.Middleware) {
	c.next = next
}
func (c *TelemetryHandler) GetNext() http.Middleware {
	return c.next
}
