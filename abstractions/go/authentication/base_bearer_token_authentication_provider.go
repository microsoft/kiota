package authentication

import (
	"errors"

	abs "github.com/microsoft/kiota/abstractions/go"
)

const authorizationHeader = "Authorization"

// Provides a base class for implementing AuthenticationProvider for Bearer token scheme.
type BaseBearerTokenAuthenticationProvider struct {
	// This method is called by the BaseBearerTokenAuthenticationProvider class to authenticate the request via the returned access token.
	// Parameters:
	//		request: Request information instance
	// Returns:
	//		string: Access token
	//		error: nil if authentication is successful, otherwise an error
	getAuthorizationToken func(request abs.RequestInformation) (string, error)
}

// Creates a new instance of the BaseBearerTokenAuthenticationProvider class.
// Parameters:
//		getAuthorizationToken: This method is called by the BaseBearerTokenAuthenticationProvider class to authenticate the request via the returned access token.
// Returns:
//		*BaseBearerTokenAuthenticationProvider: A new instance of the BaseBearerTokenAuthenticationProvider class.
func NewBaseBearerTokenAuthenticationProvider(getAuthorizationToken func(request abs.RequestInformation) (string, error)) *BaseBearerTokenAuthenticationProvider {
	return &BaseBearerTokenAuthenticationProvider{getAuthorizationToken}
}

// Authenticates the Request information instance
// Parameters:
//		request: Request information instance
// Returns:
//		error: nil if authentication is successful, otherwise an error
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
