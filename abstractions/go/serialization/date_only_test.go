package serialization

import (
	assert "github.com/stretchr/testify/assert"
	"testing"
	"time"
)

func TestItParsesADateOnly(t *testing.T) {
	dateOnly, err := ParseDateOnly("2020-01-04")
	assert.Nil(t, err)
	assert.Equal(t, "2020-01-04", dateOnly.String())
}

func TestItDoesntParseAFullDateADateOnly(t *testing.T) {
	_, err := ParseDateOnly("2020-01-04T15:04:05.00000")
	assert.NotNil(t, err)
}

func TestItCreateANewDateOnly(t *testing.T) {
	dateOnly := NewDateOnly(time.Date(2020, 1, 4, 0, 0, 0, 0, time.UTC))
	assert.Equal(t, "2020-01-04", dateOnly.String())
}
