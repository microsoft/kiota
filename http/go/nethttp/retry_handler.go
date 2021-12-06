package nethttplibrary

import (
	"math"
	nethttp "net/http"
	"strconv"
	"time"

	abs "github.com/microsoft/kiota/abstractions/go"
)

// RetryHandler handles transient HTTP responses and retries the request given the retry options
type RetryHandler struct {
	// default options to use when evaluating the response
	options RetryHandlerOptions
}

// NewRetryHandler creates a new RetryHandler with default options
func NewRetryHandler() *RetryHandler {
	return NewRetryHandlerWithOptions(RetryHandlerOptions{
		ShouldRetry: func(delay time.Duration, executionCount int, request *nethttp.Request, response *nethttp.Response) bool {
			return true
		},
	})
}

// NewRetryHandlerWithOptions creates a new RetryHandler with the given options
func NewRetryHandlerWithOptions(options RetryHandlerOptions) *RetryHandler {
	return &RetryHandler{options: options}
}

const defaultMaxRetries = 3
const absoluteMaxRetries = 10
const defaultDelaySeconds = 3
const absoluteMaxDelaySeconds = 180

// RetryHandlerOptions to apply when evaluating the response for retrial
type RetryHandlerOptions struct {
	// Callback to determine if the request should be retried
	ShouldRetry func(delay time.Duration, executionCount int, request *nethttp.Request, response *nethttp.Response) bool
	// The maximum number of times a request can be retried
	MaxRetries int
	// The delay in seconds between retries
	DelaySeconds int
}

type retryHandlerOptionsInt interface {
	abs.RequestOption
	GetShouldRetry() func(delay time.Duration, executionCount int, request *nethttp.Request, response *nethttp.Response) bool
	GetDelaySeconds() int
	GetMaxRetries() int
}

var retryKeyValue = abs.RequestOptionKey{
	Key: "RetryHandler",
}

// GetKey returns the key value to be used when the option is added to the request context
func (options *RetryHandlerOptions) GetKey() abs.RequestOptionKey {
	return retryKeyValue
}

// GetShouldRetry returns the should retry callback function which evaluates the response for retrial
func (options *RetryHandlerOptions) GetShouldRetry() func(delay time.Duration, executionCount int, request *nethttp.Request, response *nethttp.Response) bool {
	return options.ShouldRetry
}

// GetDelaySeconds returns the delays in seconds between retries
func (options *RetryHandlerOptions) GetDelaySeconds() int {
	if options.DelaySeconds < 1 {
		return defaultDelaySeconds
	} else if options.DelaySeconds > absoluteMaxDelaySeconds {
		return absoluteMaxDelaySeconds
	} else {
		return options.DelaySeconds
	}
}

// GetMaxRetries returns the maximum number of times a request can be retried
func (options *RetryHandlerOptions) GetMaxRetries() int {
	if options.MaxRetries < 1 {
		return defaultMaxRetries
	} else if options.MaxRetries > absoluteMaxRetries {
		return absoluteMaxRetries
	} else {
		return options.MaxRetries
	}
}

const retryAttemptHeader = "Retry-Attempt"
const retryAfterHeader = "Retry-After"

const tooManyRequests = 429
const serviceUnavailable = 503
const gatewayTimeout = 504

// Intercept implements the interface and evaluates whether to retry a failed request.
func (middleware RetryHandler) Intercept(pipeline Pipeline, middlewareIndex int, req *nethttp.Request) (*nethttp.Response, error) {
	response, err := pipeline.Next(req, middlewareIndex)
	if err != nil {
		return response, err
	}
	reqOption, ok := req.Context().Value(retryKeyValue).(retryHandlerOptionsInt)
	if !ok {
		reqOption = &middleware.options
	}
	return middleware.retryRequest(pipeline, middlewareIndex, reqOption, req, response, 0, 0)
}

func (middleware RetryHandler) retryRequest(pipeline Pipeline, middlewareIndex int, options retryHandlerOptionsInt, req *nethttp.Request, resp *nethttp.Response, executionCount int, cummulativeDelay time.Duration) (*nethttp.Response, error) {
	if middleware.isRetriableErrorCode(resp.StatusCode) &&
		middleware.isRetriableRequest(req) &&
		executionCount < options.GetMaxRetries() &&
		cummulativeDelay < time.Duration(absoluteMaxDelaySeconds)*time.Second &&
		options.GetShouldRetry()(cummulativeDelay, executionCount, req, resp) {
		executionCount++
		delay := middleware.getRetryDelay(req, resp, options, executionCount)
		cummulativeDelay += delay
		req.Header.Set(retryAttemptHeader, strconv.Itoa(executionCount))
		time.Sleep(delay)
		response, err := pipeline.Next(req, middlewareIndex)
		if err != nil {
			return response, err
		}
		return middleware.retryRequest(pipeline, middlewareIndex, options, req, response, executionCount, cummulativeDelay)
	}
	return resp, nil
}

func (middleware RetryHandler) isRetriableErrorCode(code int) bool {
	return code == tooManyRequests || code == serviceUnavailable || code == gatewayTimeout
}
func (middleware RetryHandler) isRetriableRequest(req *nethttp.Request) bool {
	isBodiedMethod := req.Method == "POST" || req.Method == "PUT" || req.Method == "PATCH"
	if isBodiedMethod && req.Body != nil {
		return req.ContentLength != -1
	}
	return true
}

func (middleware RetryHandler) getRetryDelay(req *nethttp.Request, resp *nethttp.Response, options retryHandlerOptionsInt, executionCount int) time.Duration {
	retryAfter := resp.Header.Get(retryAfterHeader)
	if retryAfter != "" {
		retryAfterDelay, err := strconv.ParseFloat(retryAfter, 64)
		if err == nil {
			return time.Duration(retryAfterDelay) * time.Second
		}
	} //TODO parse the header if it's a date
	return time.Duration(math.Pow(float64(options.GetDelaySeconds()), float64(executionCount))) * time.Second
}
