package authentication

import (
	assert "github.com/stretchr/testify/assert"
	u "net/url"
	"testing"
)

func TestItValidatesHosts(t *testing.T) {
	validator := NewAllowedHostsValidator([]string{"graph.microsoft.com"})
	url, err := u.Parse("https://graph.microsoft.com/v1.0/me")
	assert.Nil(t, err)
	assert.True(t, validator.IsUrlHostValid(url))
}
