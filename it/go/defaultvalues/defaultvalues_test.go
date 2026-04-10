package integrationtest

import (
	"context"
	"integrationtest/client"
	"integrationtest/client/models"
	"testing"
	"time"

	"github.com/google/uuid"
	"github.com/microsoft/kiota-abstractions-go/authentication"
	"github.com/microsoft/kiota-abstractions-go/serialization"
	http "github.com/microsoft/kiota-http-go"
)

func TestMockServerBasic(t *testing.T) {
	httpAdapter, _ := http.NewNetHttpRequestAdapter(&authentication.AnonymousAuthenticationProvider{})
	httpAdapter.SetBaseUrl("http://localhost:1080")
	client := client.NewApiClient(httpAdapter)

	//Call a sample endpoint - not really needed here.
	result, _ := client.Api().V1().WeatherForecast().Get(context.Background(), nil)
	if result == nil {
		t.Errorf("Service response is null\n")
	}
	if len(result) != 1 {
		t.Errorf("Service response does not contain one item\n")
	}

	//Now the real test: create a model class and verify that all properties have the default values.
	model := models.NewWeatherForecast()

	if *(model.GetBoolValue()) != true {
		t.Errorf("boolValue is not true\n")
	}

	if model.GetDateOnlyValue() == nil {
		t.Errorf("dateOnlyValue is null\n")
	}
	if serialization.DateOnly.String(*(model.GetDateOnlyValue())) != "1900-01-01" {
		t.Errorf("dateOnlyValue is not 1900-01-01 but %v\n", serialization.DateOnly.String(*(model.GetDateOnlyValue())))
	}

	if model.GetDateValueLocalTime() == nil {
		t.Errorf("dateValueLocalTime is null\n")
	}
	//Format the datetime in the local format:
	if (*(model.GetDateValueLocalTime())).Format("2006-01-02T15:04:05") != "1900-01-01T00:00:00" {
		t.Errorf("dateValueLocalTime is not 1900-01-01T00:00:00 but %v\n", (*(model.GetDateValueLocalTime())).Format("2006-01-02T15:04:05"))
	}
	if (*(model.GetDateValueLocalTime())).Location() != time.Now().Location() {
		t.Errorf("dateValueLocalTime does not have location %v but %v\n", (*(model.GetDateValueLocalTime())).Location(), time.Now().Location())
	}

	if *(model.GetDecimalValue()) != 25.5 {
		t.Errorf("decimalValue is not 25.5 but %v\n", *(model.GetDecimalValue()))
	}
	if *(model.GetDoubleValue()) != 25.5 {
		t.Errorf("doubleValue is not 25.5 but %v\n", *(model.GetDoubleValue()))
	}
	if *(model.GetEnumValue()) != models.ONE_WEATHERFORECAST_ENUMVALUE {
		t.Errorf("doubleValue is not %v but %v\n", models.ONE_WEATHERFORECAST_ENUMVALUE, *(model.GetEnumValue()))
	}
	if *(model.GetFloatValue()) != 25.5 {
		t.Errorf("floatValue is not 25.5 but %v\n", *(model.GetFloatValue()))
	}

	if model.GetGuidValue() == nil {
		t.Errorf("guidValue is null\n")
	}
	if uuid.UUID.String(*(model.GetGuidValue())) != "00000000-0000-0000-0000-000000000000" {
		t.Errorf("guidValue is not 00000000-0000-0000-0000-000000000000 but %v\n", uuid.UUID.String(*(model.GetGuidValue())))
	}

	if *(model.GetLongValue()) != 255 {
		t.Errorf("longValue is not 255 but %v\n", *(model.GetLongValue()))
	}
	if *(model.GetSummary()) != "Test" {
		t.Errorf("summary is not 'Test' but %v\n", *(model.GetSummary()))
	}
	if *(model.GetTemperatureC()) != 15 {
		t.Errorf("temperatureC is not 15 but %v\n", *(model.GetTemperatureC()))
	}

	if model.GetTimeValue() == nil {
		t.Errorf("timeValue is null\n")
	}
	if serialization.TimeOnly.String(*(model.GetTimeValue())) != "00:00:00" {
		t.Errorf("timeValue is not '00:00:00' but %v\n", serialization.TimeOnly.String(*(model.GetTimeValue())))
	}
}
