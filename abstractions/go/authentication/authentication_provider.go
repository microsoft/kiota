package authentication

import (
	abs "github.com/microsoft/kiota/abstractions/go"
)

// AuthenticationProvider authenticates the RequestInformation request.
type AuthenticationProvider interface {
	// AuthenticateRequest authenticates the provided RequestInformation.
	AuthenticateRequest(request abs.RequestInformation) error
}
