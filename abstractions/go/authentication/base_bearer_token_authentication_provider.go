package authentication

import (
	"errors"

	abs "github.com/microsoft/kiota/abstractions/go"
)

const authorizationHeader = "Authorization"

type BaseBearerTokenAuthenticationProvider struct {
	getAuthorizationToken func(request abs.RequestInformation) (string, error)
}

func NewBaseBearerTokenAuthenticationProvider(getAuthorizationToken func(request abs.RequestInformation) (string, error)) *BaseBearerTokenAuthenticationProvider {
	return &BaseBearerTokenAuthenticationProvider{getAuthorizationToken}
}

func (provider *BaseBearerTokenAuthenticationProvider) AuthenticateRequest(request abs.RequestInformation) error {
	if request.Headers == nil {
		request.Headers = make(map[string]string)
	}
	if provider.getAuthorizationToken == nil {
		return errors.New("This class is abstract, you need to derive from it and implement the GetAuthorizationToken method.")
	}
	if request.Headers[authorizationHeader] == "" {
		token, err := provider.getAuthorizationToken(request)
		if err != nil {
			return err
		}
		if token == "" {
			return errors.New("Could not get an authorization token")
		}
		request.Headers[authorizationHeader] = "Bearer " + token
	}

	return nil
}
