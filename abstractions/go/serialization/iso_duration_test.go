package serialization

import (
	assert "github.com/stretchr/testify/assert"
	"testing"
	"time"
)

func TestItParsesADuration(t *testing.T) {
	duration, err := ParseISODuration("PT1H")
	assert.Nil(t, err)
	assert.Equal(t, "PT1H", duration.String())
}

func TestItMakesAnISODurationFromATimeDurationFor1h(t *testing.T) {
	duration := time.Duration(1) * time.Hour
	isoDuration := FromDuration(duration)
	assert.Equal(t, "PT1H", isoDuration.String())
}

func TestItMakesAnISODurationFromATimeDurationFor1d(t *testing.T) {
	duration := time.Duration(24) * time.Hour
	isoDuration := FromDuration(duration)
	assert.Equal(t, "P1D", isoDuration.String())
}

func TestItMakesAnNewISODurationFor1h(t *testing.T) {
	isoDuration := NewDuration(0, 0, 0, 1, 0, 0, 0)
	assert.Equal(t, "PT1H", isoDuration.String())
}

func TestItMakesAnNewISODurationFor1dAnd1h(t *testing.T) {
	isoDuration := NewDuration(0, 0, 1, 1, 0, 0, 0)
	assert.Equal(t, "P1DT1H", isoDuration.String())
}

func TestItMakesAnNewISODurationFor1wAnd1dAnd1h(t *testing.T) {
	isoDuration := NewDuration(0, 1, 1, 1, 0, 0, 0)
	assert.Equal(t, "P1W1DT1H", isoDuration.String())
}

func TestItMakesAnNewISODurationFor1yAnd1dAnd1h(t *testing.T) {
	isoDuration := NewDuration(1, 0, 1, 1, 0, 0, 0)
	assert.Equal(t, "P1Y1DT1H", isoDuration.String())
}
