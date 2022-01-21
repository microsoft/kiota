package microsoft_kiota_authentication_azure

import (
	"context"
	"errors"

	u "net/url"

	azcore "github.com/Azure/azure-sdk-for-go/sdk/azcore"
	azpolicy "github.com/Azure/azure-sdk-for-go/sdk/azcore/policy"
)

// AzureIdentityAccessTokenProvider implementation of AccessTokenProvider that supports implementations of TokenCredential from Azure.Identity.
type AzureIdentityAccessTokenProvider struct {
	scopes     []string
	credential azcore.TokenCredential
}

// NewAzureIdentityAccessTokenProvider creates a new instance of the AzureIdentityAccessTokenProvider using "https://graph.microsoft.com/.default" as the default scope.
func NewAzureIdentityAccessTokenProvider(credential azcore.TokenCredential) (*AzureIdentityAccessTokenProvider, error) {
	return NewAzureIdentityAccessTokenProviderWithScopes(credential, nil)
}

// NewAzureIdentityAccessTokenProviderWithScopes creates a new instance of the AzureIdentityAccessTokenProvider.
func NewAzureIdentityAccessTokenProviderWithScopes(credential azcore.TokenCredential, scopes []string) (*AzureIdentityAccessTokenProvider, error) {
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
	result := &AzureIdentityAccessTokenProvider{
		credential: credential,
		scopes:     finalScopes,
	}

	return result, nil
}

// GetAuthorizationToken returns the access token for the provided url.
func (p *AzureIdentityAccessTokenProvider) GetAuthorizationToken(url *u.URL) (string, error) {
	options := azpolicy.TokenRequestOptions{
		Scopes: p.scopes,
	}
	token, err := p.credential.GetToken(context.Background(), options)
	if err != nil {
		return "", err
	}
	return token.Token, nil
}
