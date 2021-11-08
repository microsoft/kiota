package authentication

import abs "github.com/microsoft/kiota/abstractions/go"

// This authentication provider does not perform any authentication.
type AnonymousAuthenticationProvider struct {
}

// Authenticates the Request information instance
// Parameters:
//		request: Request information instance
// Returns:
//		error: nil if authentication is successful, otherwise an error
func (provider *AnonymousAuthenticationProvider) AuthenticateRequest(request abs.RequestInformation) error {
	return nil
}
