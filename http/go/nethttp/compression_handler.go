package nethttplibrary

import (
	"bytes"
	"compress/gzip"
	"io"
	"io/ioutil"
	"net/http"
)

type CompressionHandler struct{}

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

func (c *CompressionHandler) Intercept(pipeline Pipeline, middlewareIndex int, req *http.Request) (*http.Response, error) {
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
	defer req.Body.Close()
	req.ContentLength = int64(contentLength)

	resp, err := pipeline.Next(req, middlewareIndex)
	if err != nil {
		return nil, err
	}

	// If we get an error send uncompressed request
	if resp.StatusCode == 415 {
		delete(req.Header, "Content-Encoding")

		req.Body = ioutil.NopCloser(bytes.NewBuffer(unCompressedBody))
		defer req.Body.Close()
		req.ContentLength = unCompressedContentLength

		return pipeline.Next(req, middlewareIndex)
	}

	return resp, nil
}
