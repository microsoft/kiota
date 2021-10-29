package nethttplibrary

import (
	"errors"
	nethttp "net/http"
	"net/url"
	"strings"

	abs "github.com/microsoft/kiota/abstractions/go"
)

type RedirectHandler struct {
	options RedirectHandlerOptions
}

func NewRedirectHandler() *RedirectHandler {
	return NewRedirectHandlerWithOptions(RedirectHandlerOptions{
		MaxRedirects: DEFAULT_MAX_REDIRECTS,
		ShouldRedirect: func(req *nethttp.Request, res *nethttp.Response) bool {
			return true
		},
	})
}
func NewRedirectHandlerWithOptions(options RedirectHandlerOptions) *RedirectHandler {
	return &RedirectHandler{options: options}
}

type RedirectHandlerOptions struct {
	ShouldRedirect func(req *nethttp.Request, res *nethttp.Response) bool
	MaxRedirects   int
}

var redirectKeyValue = abs.RequestOptionKey{
	Key: "RedirectHandler",
}

type redirectHandlerOptionsInt interface {
	abs.RequestOption
	GetShouldRedirect() func(req *nethttp.Request, res *nethttp.Response) bool
	GetMaxRedirect() int
}

func (o *RedirectHandlerOptions) GetKey() abs.RequestOptionKey {
	return redirectKeyValue
}
func (o *RedirectHandlerOptions) GetShouldRedirect() func(req *nethttp.Request, res *nethttp.Response) bool {
	return o.ShouldRedirect
}
func (o *RedirectHandlerOptions) GetMaxRedirect() int {
	if o == nil || o.MaxRedirects < 1 {
		return DEFAULT_MAX_REDIRECTS
	} else if o.MaxRedirects > ABSOLUTE_MAX_REDIRECTS {
		return ABSOLUTE_MAX_REDIRECTS
	} else {
		return o.MaxRedirects
	}
}

var DEFAULT_MAX_REDIRECTS = 5
var ABSOLUTE_MAX_REDIRECTS = 20
var MOVED_PERMANENTLY = 301
var FOUND = 302
var SEE_OTHER = 303
var TEMPORARY_REDIRECT = 307
var PERMANENT_REDIRECT = 308
var LOCATION_HEADER = "Location"

func (middleware RedirectHandler) Intercept(pipeline Pipeline, req *nethttp.Request) (*nethttp.Response, error) {
	response, err := pipeline.Next(req)
	if err != nil {
		return response, err
	}
	reqOption, ok := req.Context().Value(redirectKeyValue).(redirectHandlerOptionsInt)
	if !ok {
		reqOption = &middleware.options
	}
	return middleware.redirectRequest(pipeline, reqOption, req, response, 0)
}

func (middleware RedirectHandler) redirectRequest(pipeline Pipeline, reqOption redirectHandlerOptionsInt, req *nethttp.Request, response *nethttp.Response, redirectCount int) (*nethttp.Response, error) {
	shouldRedirect := reqOption.GetShouldRedirect() != nil && reqOption.GetShouldRedirect()(req, response) || reqOption.GetShouldRedirect() == nil
	if middleware.isRedirectResponse(response) &&
		redirectCount < reqOption.GetMaxRedirect() &&
		shouldRedirect {
		redirectCount++
		redirectRequest, err := middleware.getRedirectRequest(req, response)
		if err != nil {
			return response, err
		}
		result, err := pipeline.Next(redirectRequest)
		if err != nil {
			return result, err
		}
		return middleware.redirectRequest(pipeline, reqOption, redirectRequest, result, redirectCount)
	}
	return response, nil
}

func (middleware RedirectHandler) isRedirectResponse(response *nethttp.Response) bool {
	if response == nil {
		return false
	}
	locationHeader := response.Header.Get(LOCATION_HEADER)
	if locationHeader == "" {
		return false
	}
	statusCode := response.StatusCode
	return statusCode == MOVED_PERMANENTLY || statusCode == FOUND || statusCode == SEE_OTHER || statusCode == TEMPORARY_REDIRECT || statusCode == PERMANENT_REDIRECT
}

func (middleware RedirectHandler) getRedirectRequest(request *nethttp.Request, response *nethttp.Response) (*nethttp.Request, error) {
	if request == nil || response == nil {
		return nil, errors.New("request or response is nil")
	}
	locationHeaderValue := response.Header.Get(LOCATION_HEADER)
	if locationHeaderValue == "" {
		return nil, errors.New("location header is empty")
	}
	if locationHeaderValue[0] == '/' {
		locationHeaderValue = request.URL.Scheme + "://" + request.URL.Host + locationHeaderValue
	}
	result := request.Clone(request.Context())
	targetUrl, err := url.Parse(locationHeaderValue)
	if err != nil {
		return nil, err
	}
	result.URL = targetUrl
	sameHost := strings.EqualFold(targetUrl.Host, request.URL.Host)
	sameScheme := strings.EqualFold(targetUrl.Scheme, request.URL.Scheme)
	if !sameHost || !sameScheme {
		result.Header.Del("Authorization")
	}
	if response.StatusCode == SEE_OTHER {
		result.Method = nethttp.MethodGet
		result.Header.Del("Content-Type")
		result.Header.Del("Content-Length")
		result.Body = nil
	}
	return result, nil
}
