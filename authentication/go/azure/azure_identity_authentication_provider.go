package microsoft_kiota_authentication_azure

import (
	"context"
	"errors"

	azcore "github.com/Azure/azure-sdk-for-go/sdk/azcore"
	azpolicy "github.com/Azure/azure-sdk-for-go/sdk/azcore/policy"
	abs "github.com/microsoft/kiota/abstractions/go"
	auth "github.com/microsoft/kiota/abstractions/go/authentication"
)

type AzureIdentityAuthenticationProvider struct {
	auth.BaseBearerTokenAuthenticationProvider
	scopes     []string
	credential azcore.TokenCredential
}

func NewAzureIdentityAuthenticationProvider(credential azcore.TokenCredential) (*AzureIdentityAuthenticationProvider, error) {
	return NewAzureIdentityAuthenticationProviderWithScopes(credential, nil)
}

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

	if result.scopes == nil {
		result.scopes = []string{}
	}
	if len(result.scopes) == 0 {
		result.scopes = []string{"https://graph.microsoft.com/.default"} //TODO: init from the request URL host instead for national clouds
	}

	return result, nil
}
