// Package microsoft_kiota_authentication_azure implements Kiota abstractions for authentication using the Azure Core library.
// In order to use this package, you must also add the github.com/Azure/azure-sdk-for-go/sdk/azidentity.
package microsoft_kiota_authentication_azure

import (
	"context"
	"errors"

	azcore "github.com/Azure/azure-sdk-for-go/sdk/azcore"
	azpolicy "github.com/Azure/azure-sdk-for-go/sdk/azcore/policy"
	abs "github.com/microsoft/kiota/abstractions/go"
	auth "github.com/microsoft/kiota/abstractions/go/authentication"
)

// AzureIdentityAuthenticationProvider implementation of AuthenticationProvider that supports implementations of TokenCredential from Azure.Identity.
type AzureIdentityAuthenticationProvider struct {
	auth.BaseBearerTokenAuthenticationProvider
	scopes     []string
	credential azcore.TokenCredential
}

// NewAzureIdentityAuthenticationProvider creates a new instance of the AzureIdentityAuthenticationProvider using "https://graph.microsoft.com/.default" as the default scope.
func NewAzureIdentityAuthenticationProvider(credential azcore.TokenCredential) (*AzureIdentityAuthenticationProvider, error) {
	return NewAzureIdentityAuthenticationProviderWithScopes(credential, nil)
}

// NewAzureIdentityAuthenticationProviderWithScopes creates a new instance of the AzureIdentityAuthenticationProvider.
func NewAzureIdentityAuthenticationProviderWithScopes(credential azcore.TokenCredential, scopes []string) (*AzureIdentityAuthenticationProvider, error) {
	if credential == nil {
		return nil, errors.New("credential cannot be nil")
	}
	scopesLen := len(scopes)
	finalScopes := make([]string, scopesLen)
	if scopesLen == 0 {
		finalScopes = append(finalScopes, "https://graph.microsoft.com/.default")
	} else {
		copy(finalScopes, scopes)
	}
	baseBearer := auth.NewBaseBearerTokenAuthenticationProvider(
		func(request abs.RequestInformation) (string, error) {
			options := azpolicy.TokenRequestOptions{
				Scopes: finalScopes,
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
		scopes:                                finalScopes,
	}

	return result, nil
}
