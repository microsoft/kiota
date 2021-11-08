package nethttplibrary

import (
	nethttp "net/http"
	httptest "net/http/httptest"
	testing "testing"

	"strconv"

	assert "github.com/stretchr/testify/assert"
)

func TestItCreatesANewRedirectHandler(t *testing.T) {
	handler := NewRedirectHandler()
	if handler == nil {
		t.Error("handler is nil")
	}
}

func TestItDoesntRedirectWithoutMiddleware(t *testing.T) {
	requestCount := int64(0)
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		requestCount++
		res.Header().Set("Location", "/"+strconv.FormatInt(requestCount, 10))
		res.WriteHeader(301)
		res.Write([]byte("body"))
	}))
	defer func() { testServer.Close() }()
	req, err := nethttp.NewRequest(nethttp.MethodGet, testServer.URL, nil)
	if err != nil {
		t.Error(err)
	}
	client := getDefaultClientWithoutMiddleware()
	resp, err := client.Do(req)
	if err != nil {
		t.Error(err)
	}
	assert.NotNil(t, resp)
	assert.Equal(t, int64(1), requestCount)
}

func TestItHonoursShouldRedirect(t *testing.T) {
	requestCount := int64(0)
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		requestCount++
		res.Header().Set("Location", "/"+strconv.FormatInt(requestCount, 10))
		res.WriteHeader(301)
		res.Write([]byte("body"))
	}))
	defer func() { testServer.Close() }()
	handler := NewRedirectHandlerWithOptions(RedirectHandlerOptions{
		ShouldRedirect: func(req *nethttp.Request, res *nethttp.Response) bool {
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
	assert.Equal(t, int64(1), requestCount)
}

func TestItHonoursMaxRedirect(t *testing.T) {
	requestCount := int64(0)
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		requestCount++
		res.Header().Set("Location", "/"+strconv.FormatInt(requestCount, 10))
		res.WriteHeader(301)
		res.Write([]byte("body"))
	}))
	defer func() { testServer.Close() }()
	handler := NewRedirectHandler()
	req, err := nethttp.NewRequest(nethttp.MethodGet, testServer.URL, nil)
	if err != nil {
		t.Error(err)
	}
	resp, err := handler.Intercept(newNoopPipeline(), req)
	if err != nil {
		t.Error(err)
	}
	assert.NotNil(t, resp)
	assert.Equal(t, int64(defaultMaxRedirects+1), requestCount)
}

func TestItStripsAuthorizationHeaderOnDifferentHost(t *testing.T) {
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		res.Header().Set("Location", "https://www.bing.com/")
		res.WriteHeader(301)
		res.Write([]byte("body"))
	}))
	defer func() { testServer.Close() }()
	handler := NewRedirectHandler()
	req, err := nethttp.NewRequest(nethttp.MethodGet, testServer.URL, nil)
	if err != nil {
		t.Error(err)
	}
	req.Header.Set("Authorization", "Bearer 12345")
	client := getDefaultClientWithoutMiddleware()
	resp, err := client.Do(req)
	if err != nil {
		t.Error(err)
	}
	result, err := handler.getRedirectRequest(req, resp)
	if err != nil {
		t.Error(err)
	}
	assert.NotNil(t, result)
	assert.Equal(t, "", result.Header.Get("Authorization"))
}
