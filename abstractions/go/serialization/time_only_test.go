package serialization

import (
	assert "github.com/stretchr/testify/assert"
	"testing"
	"time"
)

func TestItParsesATimeOnly(t *testing.T) {
	dateOnly, err := ParseTimeOnly("16:20:21.000")
	assert.Nil(t, err)
	assert.Equal(t, "16:20:21.000000000", dateOnly.String())
}

func TestItParsesATimeOnlyNoDecimals(t *testing.T) {
	dateOnly, err := ParseTimeOnly("16:20:21")
	assert.Nil(t, err)
	assert.Equal(t, "16:20:21.000000000", dateOnly.String())
}

func TestItDoesntParseATimeOnlyWithTooManyDecimals(t *testing.T) {
	_, err := ParseTimeOnly("2020-01-04T15:04:05.00000000000000000000")
	assert.NotNil(t, err)
}

func TestItDoesntParseAFullDateATimeOnly(t *testing.T) {
	_, err := ParseTimeOnly("2020-01-04T15:04:05.00000")
	assert.NotNil(t, err)
}

func TestItCreateANewTimeOnly(t *testing.T) {
	dateOnly := NewTimeOnly(time.Date(1, 1, 1, 16, 20, 21, 0, time.UTC))
	assert.Equal(t, "16:20:21", dateOnly.String())
}
