package authentication

import (
	"errors"

	abs "github.com/microsoft/kiota/abstractions/go"
)

const authorizationHeader = "Authorization"

// BaseBearerTokenAuthenticationProvider provides a base class implementing AuthenticationProvider for Bearer token scheme.
type BaseBearerTokenAuthenticationProvider struct {
	// getAuthorizationToken is called by the BaseBearerTokenAuthenticationProvider class to authenticate the request via the returned access token.
	getAuthorizationToken func(request abs.RequestInformation) (string, error)
}

// NewBaseBearerTokenAuthenticationProvider creates a new instance of the BaseBearerTokenAuthenticationProvider class.
func NewBaseBearerTokenAuthenticationProvider(getAuthorizationToken func(request abs.RequestInformation) (string, error)) *BaseBearerTokenAuthenticationProvider {
	return &BaseBearerTokenAuthenticationProvider{getAuthorizationToken}
}

// AuthenticateRequest authenticates the provided RequestInformation instance using the provided authorization token callback.
func (provider *BaseBearerTokenAuthenticationProvider) AuthenticateRequest(request abs.RequestInformation) error {
	if request.Headers == nil {
		request.Headers = make(map[string]string)
	}
	if provider.getAuthorizationToken == nil {
		return errors.New("this class is abstract, you need to derive from it and implement the GetAuthorizationToken method.")
	}
	if request.Headers[authorizationHeader] == "" {
		token, err := provider.getAuthorizationToken(request)
		if err != nil {
			return err
		}
		if token == "" {
			return errors.New("could not get an authorization token")
		}
		request.Headers[authorizationHeader] = "Bearer " + token
	}

	return nil
}
