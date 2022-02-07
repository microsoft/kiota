package authentication

import (
	u "net/url"
)

//AccessTokenProvider returns access tokens.
type AccessTokenProvider interface {
	// GetAuthorizationToken returns the access token for the provided url.
	GetAuthorizationToken(url *u.URL) (string, error)
	// GetAllowedHostsValidator returns the hosts validator.
	GetAllowedHostsValidator() *AllowedHostsValidator
}
