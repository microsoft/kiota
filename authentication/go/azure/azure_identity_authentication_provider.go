package microsoft_kiota_authentication_azure

import (
	"context"
	"errors"

	azcore "github.com/Azure/azure-sdk-for-go/sdk/azcore"
	azpolicy "github.com/Azure/azure-sdk-for-go/sdk/azcore/policy"
	abs "github.com/microsoft/kiota/abstractions/go"
	auth "github.com/microsoft/kiota/abstractions/go/authentication"
)

// The BaseBearerTokenAuthenticationProvider implementation that supports implementations of TokenCredential from Azure.Identity.
type AzureIdentityAuthenticationProvider struct {
	auth.BaseBearerTokenAuthenticationProvider
	scopes     []string
	credential azcore.TokenCredential
}

// Creates a new instance of the AzureIdentityAuthenticationProvider.
// Parameters:
//   credential: The TokenCredential implementation that will be used to acquire tokens.
// Returns:
//   - The new instance of the AzureIdentityAuthenticationProvider.
//   - An error if any.
func NewAzureIdentityAuthenticationProvider(credential azcore.TokenCredential) (*AzureIdentityAuthenticationProvider, error) {
	return NewAzureIdentityAuthenticationProviderWithScopes(credential, nil)
}

// Creates a new instance of the AzureIdentityAuthenticationProvider.
// Parameters:
//   credential: The TokenCredential implementation that will be used to acquire tokens.
//   scopes: The list of scopes that will be used to acquire the token.
// Returns:
//   - The new instance of the AzureIdentityAuthenticationProvider.
//   - An error if any.
func NewAzureIdentityAuthenticationProviderWithScopes(credential azcore.TokenCredential, scopes []string) (*AzureIdentityAuthenticationProvider, error) {
	if credential == nil {
		return nil, errors.New("credential cannot be nil")
	}
	baseBearer := auth.NewBaseBearerTokenAuthenticationProvider(
		func(request abs.RequestInformation) (string, error) {
			options := azpolicy.TokenRequestOptions{
				Scopes: scopes,
			}
			token, err := credential.GetToken(context.Background(), options)
			if err != nil {
				return "", err
			}
			return token.Token, nil
		})
	result := &AzureIdentityAuthenticationProvider{
		BaseBearerTokenAuthenticationProvider: *baseBearer,
		credential:                            credential,
		scopes:                                scopes,
	}

	if result.scopes == nil || len(result.scopes) == 0 {
		result.scopes = []string{"https://graph.microsoft.com/.default"} //TODO: init from the request URL host instead for national clouds
	}

	return result, nil
}
