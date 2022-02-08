package nethttplibrary

import (
	"compress/gzip"
	"encoding/json"
	"io/ioutil"
	nethttp "net/http"
	httptest "net/http/httptest"
	"testing"

	"github.com/stretchr/testify/assert"
)

func TestTransportDecompressesResponse(t *testing.T) {
	result := map[string]string{"name": "Test", "email": "Test@Test.com"}

	testServer := httptest.NewServer(nethttp.HandlerFunc(func(res nethttp.ResponseWriter, req *nethttp.Request) {
		postBody, _ := json.Marshal(result)
		res.Header().Set("Content-Type", "application/json")
		res.Header().Set("Content-Encoding", "gzip")

		gz := gzip.NewWriter(res)
		defer gz.Close()

		gz.Write(postBody)
	}))
	defer testServer.Close()

	client := getDefaultClientWithoutMiddleware()
	client.Transport = NewCustomTransport(NewCompressionHandler())

	resp, _ := client.Get(testServer.URL)
	respBody, _ := ioutil.ReadAll(resp.Body)

	assert.True(t, resp.Uncompressed)
	assert.Equal(t, string(respBody), `{"email":"Test@Test.com","name":"Test"}`)
}
