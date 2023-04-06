package integrationtest

import (
	"context"
	"errors"
	"integrationtest/client"
	"integrationtest/client/models"
	"testing"

	"github.com/microsoft/kiota-abstractions-go/authentication"
	http "github.com/microsoft/kiota-http-go"
)

func TestMockServerBasic(t *testing.T) {
	httpAdapter, _ := http.NewNetHttpRequestAdapter(&authentication.AnonymousAuthenticationProvider{})
	httpAdapter.SetBaseUrl("http://localhost:1080")
	client := client.NewApiClient(httpAdapter)

	_, err := client.Api().V1().Topics().Get(context.Background(), nil)

	target := &models.Error{}
	if errors.As(err, &target) {
		if *target.GetId() != "my-sample-id" {
			t.Errorf("Error Id incorrect %v\n", *target.GetId())
		}
		if *target.GetCode() != 123 {
			t.Errorf("Error Code incorrect %v\n", *target.GetCode())
		}
	} else {
		t.Errorf("Error is an incorrect type %v\n", err)
	}
}
