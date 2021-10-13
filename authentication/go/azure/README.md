# To-do

![Go](https://github.com/microsoft/kiota/actions/workflows/authentication-go-azure.yml/badge.svg)

- [ ] unit tests
- [ ] move to its own repo and implement [the guidelines](https://golang.org/doc/#developing-modules) to make referencing the module easier
- [ ] add doc.go
- [ ] rename module name, update reference and remove the replace directive

## Using the Azure Authentication library

1. Navigate to the directory where `go.mod` is located for your project.
1. Run the following command:

    ```Shell
    go get github.com/microsoft/kiota/authentication/go/azure
    ```

1. In the code

    ```Golang
    cred, err := azidentity.NewDeviceCodeCredential(nil)
    authProvider, err := kiotaazure.NewAzureIdentityAuthenticationProviderWithScopes(cred, []string{"User.Read"})
    ```
