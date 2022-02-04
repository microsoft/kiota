package nethttplibrary

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	nethttp "net/http"
	httptest "net/http/httptest"
	"testing"

	"github.com/stretchr/testify/assert"
)

func TestCompressionHandlerAddsAcceptEncodingHeader(t *testing.T) {
	postBody, _ := json.Marshal(map[string]string{"name": "Test", "email": "Test@Test.com"})
	var acceptEncodingHeader string
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		acceptEncodingHeader = req.Header.Get("Accept-Encoding")
		fmt.Fprint(res, `{}`)
	}))
	defer testServer.Close()

	client := GetDefaultClient(&CompressionHandler{})
	client.Post(testServer.URL, "application/json", bytes.NewBuffer(postBody))

	assert.Equal(t, acceptEncodingHeader, "gzip")
}

func TestCompressionHandlerAddsContentEncodingHeader(t *testing.T) {
	postBody, _ := json.Marshal(map[string]string{"name": "Test", "email": "Test@Test.com"})
	var contentTypeHeader string
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		contentTypeHeader = req.Header.Get("Content-Encoding")
		fmt.Fprint(res, `{}`)
	}))
	defer testServer.Close()

	client := GetDefaultClient(&CompressionHandler{})
	client.Post(testServer.URL, "application/json", bytes.NewBuffer(postBody))

	assert.Equal(t, contentTypeHeader, "gzip")
}

func TestCompressionHandlerCompressesRequestBody(t *testing.T) {
	postBody, _ := json.Marshal(map[string]string{"name": "Test", "email": "Test@Test.com"})
	var compressedBody []byte

	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		compressedBody, _ = io.ReadAll(req.Body)
		res.Header().Set("Content-Type", "application/json")
		fmt.Fprint(res, `{}`)
	}))
	defer testServer.Close()

	client := getDefaultClientWithoutMiddleware()
	client.Transport = NewCustomTransport(&CompressionHandler{})
	client.Post(testServer.URL, "application/json", bytes.NewBuffer(postBody))

	assert.Greater(t, len(postBody), len(compressedBody))
}

func TestCompressionHandlerRetriesRequest(t *testing.T) {
	postBody, _ := json.Marshal(map[string]string{"name": "Test", "email": "Test@Test.com"})
	status := 415
	reqCount := 0

	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		defer req.Body.Close()
		res.Header().Set("Content-Type", "application/json")
		res.WriteHeader(status)
		status = 200
		reqCount += 1
		fmt.Fprint(res, `{}`)
	}))
	defer testServer.Close()

	client := getDefaultClientWithoutMiddleware()
	client.Transport = NewCustomTransport(&CompressionHandler{})
	client.Post(testServer.URL, "application/json", bytes.NewBuffer(postBody))

	assert.Equal(t, reqCount, 2)
}

func TestResetTransport(t *testing.T) {
	client := getDefaultClientWithoutMiddleware()
	client.Transport = &nethttp.Transport{}
}
