package authentication

import (
	abs "github.com/microsoft/kiota/abstractions/go"
)

type AuthenticationProvider interface {
	Authenticate(request abs.RequestInfo) error
}
