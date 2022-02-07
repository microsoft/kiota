package nethttplibrary

import (
	assert "github.com/stretchr/testify/assert"
	"net/http"
	"testing"
)

type TestMiddleware struct{}

func (middleware TestMiddleware) Intercept(pipeline Pipeline, middlewareIndex int, req *http.Request) (*http.Response, error) {
	req.Header.Add("test", "test-header")

	return pipeline.Next(req, middlewareIndex)
}

func TestCanInterceptRequests(t *testing.T) {
	transport := NewCustomTransport(&TestMiddleware{})
	client := &http.Client{Transport: transport}
	resp, _ := client.Get("https://example.com")

	expect := "test-header"
	got := resp.Request.Header.Get("test")

	if expect != got {
		t.Errorf("Expected: %v, but received: %v", expect, got)
	}
}

func TestCanInterceptMultipleRequests(t *testing.T) {
	transport := NewCustomTransport(&TestMiddleware{})
	client := &http.Client{Transport: transport}
	resp, _ := client.Get("https://example.com")

	expect := "test-header"
	got := resp.Request.Header.Get("test")

	if expect != got {
		t.Errorf("Expected: %v, but received: %v", expect, got)
	}

	resp2, _ := client.Get("https://example.com")

	got2 := resp2.Request.Header.Get("test")

	if expect != got2 {
		t.Errorf("Expected: %v, but received: %v", expect, got2)
	}
}

func TestItReturnsADefaultTransport(t *testing.T) {
	transport := GetDefaultTransport()
	assert.NotNil(t, transport)
	defaultTransport, ok := transport.(*http.Transport)
	assert.True(t, ok)
	assert.True(t, defaultTransport.ForceAttemptHTTP2)
}

func TestItAcceptsACustomizedTransport(t *testing.T) {
	transport := http.DefaultTransport.(*http.Transport).Clone()
	transport.ForceAttemptHTTP2 = false
	customTransport := NewCustomTransportWithParentTransport(transport)
	assert.NotNil(t, customTransport)
	result, ok := customTransport.middlewarePipeline.transport.(*http.Transport)
	assert.True(t, ok)
	assert.False(t, result.ForceAttemptHTTP2)
}

func TestItGetsADefaultTransportIfNoneIsProvided(t *testing.T) {
	customTransport := NewCustomTransport()
	assert.NotNil(t, customTransport.middlewarePipeline.transport)
}
