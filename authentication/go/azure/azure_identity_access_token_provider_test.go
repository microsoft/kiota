package microsoft_kiota_authentication_azure

import (
	"context"
	azcore "github.com/Azure/azure-sdk-for-go/sdk/azcore"
	policy "github.com/Azure/azure-sdk-for-go/sdk/azcore/policy"
	assert "github.com/stretchr/testify/assert"
	u "net/url"
	"testing"
)

type MockTokenCredential struct {
	TokenValue string
}

func (m *MockTokenCredential) GetToken(ctx context.Context, options policy.TokenRequestOptions) (*azcore.AccessToken, error) {
	return &azcore.AccessToken{
		Token: m.TokenValue,
	}, nil
}

func TestAddsTokenOnValidHost(t *testing.T) {
	provider, err := NewAzureIdentityAccessTokenProvider(&MockTokenCredential{TokenValue: "token"})
	assert.Nil(t, err)
	assert.NotNil(t, provider)

	token, err := provider.GetAuthorizationToken(&u.URL{Host: "graph.microsoft.com", Scheme: "https"})
	assert.Nil(t, err)
	assert.Equal(t, "token", token)
}

func TestAddsTokenOnValidHostFromParse(t *testing.T) {
	provider, err := NewAzureIdentityAccessTokenProvider(&MockTokenCredential{TokenValue: "token"})
	assert.Nil(t, err)
	assert.NotNil(t, provider)

	url, err := u.Parse("https://graph.microsoft.com")
	assert.Nil(t, err)

	token, err := provider.GetAuthorizationToken(url)
	assert.Nil(t, err)
	assert.Equal(t, "token", token)
}

func TestDoesntAddTokenOnDifferentHost(t *testing.T) {
	provider, err := NewAzureIdentityAccessTokenProvider(&MockTokenCredential{TokenValue: "token"})
	assert.Nil(t, err)
	assert.NotNil(t, provider)

	token, err := provider.GetAuthorizationToken(&u.URL{Host: "differenthost.com"})
	assert.Nil(t, err)
	assert.Empty(t, token)
}

func TestDoesntAddTokenOnHttp(t *testing.T) {
	provider, err := NewAzureIdentityAccessTokenProvider(&MockTokenCredential{TokenValue: "token"})
	assert.Nil(t, err)
	assert.NotNil(t, provider)

	token, err := provider.GetAuthorizationToken(&u.URL{Host: "differenthost.com", Scheme: "http"})
	assert.Nil(t, err)
	assert.Empty(t, token)
}
