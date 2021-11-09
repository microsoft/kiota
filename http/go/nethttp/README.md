# To-do

![Go](https://github.com/microsoft/kiota/actions/workflows/http-go-nethttp.yml/badge.svg)

- [ ] unit tests
- [ ] move to its own repo and implement [the guidelines](https://golang.org/doc/#developing-modules) to make referencing the module easier
- [ ] add doc.go
- [ ] rename module name, update reference and remove the replace directive

## Using the net http implementation

1. Navigate to the directory where `go.mod` is located for your project.
1. Run the following command:

    ```Shell
    go get github.com/microsoft/kiota/http/go/nethttp
    ```

1. Add the following code

    ```Golang
    httpAdapter, err := NewNetHttpRequestAdapter(authProvider)
    ```
