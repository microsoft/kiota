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

	client := GetDefaultClient(NewCompressionHandler())
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

	client := GetDefaultClient(NewCompressionHandler())
	client.Post(testServer.URL, "application/json", bytes.NewBuffer(postBody))

	assert.Equal(t, contentTypeHeader, "gzip")
}

func TestCopmressionHandlerCopmressesRequestBody(t *testing.T) {
	postBody, _ := json.Marshal(map[string]string{"name": `Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has Contrary to popular belief, Lorem Ipsum is not simply random text. It has roots in a piece of classical Latin literature from 45 BC, making it over 2000 years old. Richard McClintock, a Latin professor at Hampden-Sydney College in Virginia, looked up one of the more obscure Latin words, consectetur, from a Lorem Ipsum passage, and going through the cites of the word in classical literature, discovered the undoubtable source. Lorem Ipsum comes from sections 1.10.32 and 1.10.33 of "de Finibus Bonorum et Malorum" (The Extremes of Good and Evil) by Cicero, written in 45 BC. This book is a treatise on the theory of ethics, very popular during the Renaissance. The first line of Lorem Ipsum, "Lorem ipsum dolor sit amet..", comes from Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.a line in section 1.10.32.
    `, "email": "Test@Test.com"})
	var compressedBody []byte
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		compressedBody, _ = io.ReadAll(req.Body)
		fmt.Fprint(res, `{}`)
	}))
	defer testServer.Close()

	client := GetDefaultClient(NewCompressionHandler())
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
	client.Transport = NewCustomTransport(NewCompressionHandler())
	client.Post(testServer.URL, "application/json", bytes.NewBuffer(postBody))

	assert.Equal(t, reqCount, 2)
}

func TestCompressionHandlerWorksWithEmptyBody(t *testing.T) {
	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		result, _ := json.Marshal(map[string]string{"name": "Test", "email": "Test@Test.com"})

		res.Header().Set("Content-Type", "application/json")
		res.Header().Set("Content-Encoding", "gzip")
		fmt.Fprint(res, result)
	}))
	defer testServer.Close()

	client := getDefaultClientWithoutMiddleware()
	client.Transport = NewCustomTransport(NewCompressionHandler())

	fmt.Print(testServer.URL)
	resp, _ := client.Get(testServer.URL)

	assert.NotNil(t, resp)
}

func TestResetTransport(t *testing.T) {
	client := getDefaultClientWithoutMiddleware()
	client.Transport = &nethttp.Transport{}
}
