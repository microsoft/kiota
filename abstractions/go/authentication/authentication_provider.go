package authentication

import (
	abs "github.com/microsoft/kiota/abstractions/go"
)

// Authenticates the application request.
type AuthenticationProvider interface {
	// Authenticates the Request information instance
	// Parameters:
	//		request: Request information instance
	// Returns:
	//		error: nil if authentication is successful, otherwise an error
	AuthenticateRequest(request abs.RequestInformation) error
}
