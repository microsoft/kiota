package nethttplibrary

import (
	nethttp "net/http"
	httptest "net/http/httptest"
	testing "testing"
	"time"

	"strconv"

	assert "github.com/stretchr/testify/assert"
)

type NoopPipeline struct {
	client *nethttp.Client
}

func (pipeline *NoopPipeline) Next(req *nethttp.Request) (*nethttp.Response, error) {
	return pipeline.client.Do(req)
}
func newNoopPipeline() *NoopPipeline {
	return &NoopPipeline{
		client: getDefaultClientWithoutMiddleware(),
	}
}
func TestItCreatesANewRetryHandler(t *testing.T) {
	handler := NewRetryHandler()
	if handler == nil {
		t.Error("handler is nil")
	}
}
func TestItAddsRetryAttemptHeaders(t *testing.T) {
	retryAttemptInt := 0
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		retryAttempt := req.Header.Get("Retry-Attempt")
		if retryAttempt == "" {
			res.WriteHeader(429)
		} else {
			res.WriteHeader(200)
			retryAttemptInt, _ = strconv.Atoi(retryAttempt)
		}
		res.Write([]byte("body"))
	}))
	defer func() { testServer.Close() }()
	handler := NewRetryHandler()
	req, err := nethttp.NewRequest(nethttp.MethodGet, testServer.URL, nil)
	if err != nil {
		t.Error(err)
	}
	resp, err := handler.Intercept(newNoopPipeline(), req)
	if err != nil {
		t.Error(err)
	}
	assert.NotNil(t, resp)
	assert.Equal(t, 1, retryAttemptInt)
}

func TestItHonoursShouldRetry(t *testing.T) {
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		retryAttempt := req.Header.Get("Retry-Attempt")
		if retryAttempt == "" {
			res.WriteHeader(429)
		} else {
			res.WriteHeader(200)
		}
		res.Write([]byte("body"))
	}))
	defer func() { testServer.Close() }()
	handler := NewRetryHandlerWithOptions(RetryHandlerOptions{
		ShouldRetry: func(delay time.Duration, executionCount int, request *nethttp.Request, response *nethttp.Response) bool {
			return false
		},
	})
	req, err := nethttp.NewRequest(nethttp.MethodGet, testServer.URL, nil)
	if err != nil {
		t.Error(err)
	}
	resp, err := handler.Intercept(newNoopPipeline(), req)
	if err != nil {
		t.Error(err)
	}
	assert.NotNil(t, resp)
	assert.Equal(t, 429, resp.StatusCode)
}

func TestItHonoursMaxRetries(t *testing.T) {
	retryAttemptInt := -1
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		res.WriteHeader(429)
		retryAttemptInt++
		res.Write([]byte("body"))
	}))
	defer func() { testServer.Close() }()
	handler := NewRetryHandler()
	req, err := nethttp.NewRequest(nethttp.MethodGet, testServer.URL, nil)
	if err != nil {
		t.Error(err)
	}
	resp, err := handler.Intercept(newNoopPipeline(), req)
	if err != nil {
		t.Error(err)
	}
	assert.NotNil(t, resp)
	assert.Equal(t, 429, resp.StatusCode)
	assert.Equal(t, defaultMaxRetries, retryAttemptInt)
}

func TestItDoesntRetryOnSuccess(t *testing.T) {
	retryAttemptInt := -1
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		res.WriteHeader(200)
		retryAttemptInt++
		res.Write([]byte("body"))
	}))
	defer func() { testServer.Close() }()
	handler := NewRetryHandler()
	req, err := nethttp.NewRequest(nethttp.MethodGet, testServer.URL, nil)
	if err != nil {
		t.Error(err)
	}
	resp, err := handler.Intercept(newNoopPipeline(), req)
	if err != nil {
		t.Error(err)
	}
	assert.NotNil(t, resp)
	assert.Equal(t, 0, retryAttemptInt)
}
