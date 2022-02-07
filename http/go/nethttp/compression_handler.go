package nethttplibrary

import (
	"bytes"
	"compress/gzip"
	"io"
	"io/ioutil"
	"net/http"

	abstractions "github.com/microsoft/kiota/abstractions/go"
)

type CompressionHandler struct {
	options CompressionOptions
}

type CompressionOptions struct {
	enableCompression bool
}

type compression interface {
	abstractions.RequestOption
	ShouldCompress() bool
}

var compressKey = abstractions.RequestOptionKey{Key: "CompressionHandler"}

func NewCompressionHandler() *CompressionHandler {
	options := NewCompressionOptions(true)
	return NewCompressionHandlerWithOptions(options)
}

func NewCompressionHandlerWithOptions(option CompressionOptions) *CompressionHandler {
	return &CompressionHandler{options: option}
}

func NewCompressionOptions(enableCompression bool) CompressionOptions {
	return CompressionOptions{enableCompression: enableCompression}
}

func (o CompressionOptions) GetKey() abstractions.RequestOptionKey {
	return compressKey
}

func (o CompressionOptions) ShouldCompress() bool {
	return o.enableCompression
}

func (c *CompressionHandler) Intercept(pipeline Pipeline, middlewareIndex int, req *http.Request) (*http.Response, error) {
	reqOption, ok := req.Context().Value(compressKey).(compression)
	if !ok {
		reqOption = c.options
	}

	if reqOption.ShouldCompress() != true {
		return pipeline.Next(req, middlewareIndex)
	}

	unCompressedBody, err := ioutil.ReadAll(req.Body)
	unCompressedContentLength := req.ContentLength
	if err != nil {
		return nil, err
	}

	compressedBody, contentLength, err := compressReqBody(req.Body)
	if err != nil {
		return nil, err
	}

	req.Header.Set("Content-Encoding", "gzip")
	req.Header.Set("Accept-Encoding", "gzip")
	req.Body = compressedBody
	req.ContentLength = int64(contentLength)

	// Sending request with compressed body
	resp, err := pipeline.Next(req, middlewareIndex)
	if err != nil {
		return nil, err
	}

	// If response has status 415 retry request with uncompressed body
	if resp.StatusCode == 415 {
		delete(req.Header, "Content-Encoding")
		req.Body = ioutil.NopCloser(bytes.NewBuffer(unCompressedBody))
		req.ContentLength = unCompressedContentLength

		return pipeline.Next(req, middlewareIndex)
	}

	return resp, nil
}

func compressReqBody(reqBody io.ReadCloser) (io.ReadCloser, int, error) {
	body, err := ioutil.ReadAll(reqBody)
	if err != nil {
		return nil, 0, err
	}

	var buffer bytes.Buffer
	gzipWriter := gzip.NewWriter(&buffer)
	if _, err := gzipWriter.Write(body); err != nil {
		return nil, 0, err
	}
	defer gzipWriter.Close()

	return ioutil.NopCloser(bytes.NewBuffer(buffer.Bytes())), len(buffer.Bytes()), nil
}
