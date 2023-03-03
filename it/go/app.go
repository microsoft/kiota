package integrationtest

import (
	c "integrationtest/client"
	azidentity "github.com/Azure/azure-sdk-for-go/sdk/azidentity"
	a "github.com/microsoft/kiota-authentication-azure-go"
	r "github.com/microsoft/kiota-http-go"
    "fmt"
    "context"
)

func main() {
	cred, err := azidentity.NewDeviceCodeCredential(&azidentity.DeviceCodeCredentialOptions{
		TenantID: "68B090C3-BA6F-472D-B29A-DB87846DB5BB",
		ClientID: "FE821132-449F-48EE-BC64-A8C1D9557215",
		UserPrompt: func(ctx context.Context, message azidentity.DeviceCodeMessage) error {
			fmt.Println(message.Message)
			return nil
		},
	})
	if err != nil {
		fmt.Printf("Error creating credentials: %v\n", err)
	}
	auth, err := a.NewAzureIdentityAuthenticationProviderWithScopes(cred, []string{"Mail.Read", "Mail.Send"})
	if err != nil {
		fmt.Printf("Error authentication provider: %v\n", err)
		return
	}
	adapter, err := r.NewNetHttpRequestAdapter(auth)
	if err != nil {
		fmt.Printf("Error creating adapter: %v\n", err)
		return
	}
	client := c.NewApiClient(adapter)
	fmt.Printf("Message: %v\n", client)
}