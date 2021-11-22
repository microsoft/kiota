package authentication

import abs "github.com/microsoft/kiota/abstractions/go"

// AnonymousAuthenticationProvider implements the AuthenticationProvider interface does not perform any authentication.
type AnonymousAuthenticationProvider struct {
}

// AuthenticateRequest is a placeholder method that "authenticates" the RequestInformation instance: no-op.
func (provider *AnonymousAuthenticationProvider) AuthenticateRequest(request abs.RequestInformation) error {
	return nil
}
