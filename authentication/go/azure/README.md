# Azure Identity Authentication provider for Kiota Go

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
