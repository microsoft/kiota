package authentication

import (
	abs "github.com/microsoft/kiota/abstractions/go"
)

type AuthenticationProvider interface {
	AuthenticateRequest(request abs.RequestInformation) error
}
