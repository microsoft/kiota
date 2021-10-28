package nethttplibrary

import (
	"math"
	nethttp "net/http"
	"strconv"
	"time"

	abs "github.com/microsoft/kiota/abstractions/go"
)

type RetryHandler struct {
	options RetryHandlerOptions
}

func NewRetryHandler() *RetryHandler {
	return NewRetryHandlerWithOptions(RetryHandlerOptions{
		ShouldRetry: func(delay time.Duration, executionCount int, request *nethttp.Request, response *nethttp.Response) bool {
			return true
		},
	})
}

func NewRetryHandlerWithOptions(options RetryHandlerOptions) *RetryHandler {
	return &RetryHandler{options: options}
}

var DEFAULT_MAX_RETRIES = 3
var ABSOLUTE_MAX_RETRIES = 10
var DEFAULT_DELAY_SECONDS = 3
var ABSOLUTE_MAX_DELAY_SECONDS = 180

type RetryHandlerOptions struct {
	ShouldRetry  func(delay time.Duration, executionCount int, request *nethttp.Request, response *nethttp.Response) bool
	MaxRetries   int
	DelaySeconds int
}

type retryHandlerOptionsInt interface {
	abs.RequestOption
	GetShouldRetry() func(delay time.Duration, executionCount int, request *nethttp.Request, response *nethttp.Response) bool
	GetDelaySeconds() int
	GetMaxRetries() int
}

var keyValue = abs.RequestOptionKey{
	Key: "RetryHandler",
}

func (o *RetryHandlerOptions) GetKey() abs.RequestOptionKey {
	return keyValue
}
func (o *RetryHandlerOptions) GetShouldRetry() func(delay time.Duration, executionCount int, request *nethttp.Request, response *nethttp.Response) bool {
	return o.ShouldRetry
}
func (o *RetryHandlerOptions) GetDelaySeconds() int {
	if o.DelaySeconds < 1 {
		return DEFAULT_DELAY_SECONDS
	} else if o.DelaySeconds > ABSOLUTE_MAX_DELAY_SECONDS {
		return ABSOLUTE_MAX_DELAY_SECONDS
	} else {
		return o.DelaySeconds
	}
}
func (o *RetryHandlerOptions) GetMaxRetries() int {
	if o.MaxRetries < 1 {
		return DEFAULT_MAX_RETRIES
	} else if o.MaxRetries > ABSOLUTE_MAX_RETRIES {
		return ABSOLUTE_MAX_RETRIES
	} else {
		return o.MaxRetries
	}
}

const RETRY_ATTEMPT_HEADER = "Retry-Attempt"
const RETRY_AFTER_HEADER = "Retry-After"

var TOO_MANY_REQUESTS = 429
var SERVICE_UNAVAILABLE = 503
var GATEWAY_TIMEOUT = 504

func (middleware RetryHandler) Intercept(pipeline Pipeline, req *nethttp.Request) (*nethttp.Response, error) {
	response, err := pipeline.Next(req)
	if err != nil {
		return response, err
	}
	reqOption, ok := req.Context().Value(keyValue).(retryHandlerOptionsInt)
	if !ok {
		reqOption = &middleware.options
	}
	return middleware.retryRequest(pipeline, reqOption, req, response, 0, 0)
}

func (middleware RetryHandler) retryRequest(pipeline Pipeline, options retryHandlerOptionsInt, req *nethttp.Request, resp *nethttp.Response, executionCount int, cummulativeDelay time.Duration) (*nethttp.Response, error) {
	if middleware.isRetriableErrorCode(resp.StatusCode) &&
		middleware.isRetriableRequest(req) &&
		executionCount < options.GetMaxRetries() &&
		cummulativeDelay < time.Duration(ABSOLUTE_MAX_DELAY_SECONDS)*time.Second &&
		options.GetShouldRetry()(cummulativeDelay, executionCount, req, resp) {
		executionCount++
		delay := middleware.getRetryDelay(req, resp, options, executionCount)
		cummulativeDelay += delay
		req.Header.Set(RETRY_ATTEMPT_HEADER, strconv.Itoa(executionCount))
		time.Sleep(delay)
		response, err := pipeline.Next(req)
		if err != nil {
			return response, err
		}
		return middleware.retryRequest(pipeline, options, req, response, executionCount, cummulativeDelay)
	}
	return resp, nil
}

func (middleware RetryHandler) isRetriableErrorCode(code int) bool {
	return code == TOO_MANY_REQUESTS || code == SERVICE_UNAVAILABLE || code == GATEWAY_TIMEOUT
}
func (middleware RetryHandler) isRetriableRequest(req *nethttp.Request) bool {
	isBodiedMethod := req.Method == "POST" || req.Method == "PUT" || req.Method == "PATCH"
	if isBodiedMethod && req.Body != nil {
		return req.ContentLength != -1
	}
	return true
}

func (middleware RetryHandler) getRetryDelay(req *nethttp.Request, resp *nethttp.Response, options retryHandlerOptionsInt, executionCount int) time.Duration {
	retryAfter := resp.Header.Get(RETRY_AFTER_HEADER)
	if retryAfter != "" {
		retryAfterDelay, err := strconv.ParseFloat(retryAfter, 64)
		if err == nil {
			return time.Duration(retryAfterDelay) * time.Second
		}
	} //TODO parse the header if it's a date
	return time.Duration(math.Pow(float64(options.GetDelaySeconds()), float64(executionCount))) * time.Second
}
