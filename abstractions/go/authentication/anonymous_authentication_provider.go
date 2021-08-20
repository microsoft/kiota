package authentication

import abs "github.com/microsoft/kiota/abstractions/go"

type AnonymousAuthenticationProvider struct {
}

func (provider *AnonymousAuthenticationProvider) Authenticate(request abs.RequestInfo) error {
	return nil
}
