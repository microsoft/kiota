package abstractions

import (
	"errors"
	"reflect"
)

// The base implementation of the Query Parameters
type QueryParametersBase struct {
}

// Vanity method to add the query parameters to the request query parameters dictionary.
// Parameters:
//  - target: The target map to add the query parameters to.
// Returns:
//  - error: An error if the target is nil.
func (p *QueryParametersBase) AddQueryParameters(target map[string]string) error {
	if target == nil {
		return errors.New("target cannot be nil")
	}
	valOfP := reflect.ValueOf(p).Elem()
	typeOfP := valOfP.Type()
	numOfFields := valOfP.NumField()
	for i := 0; i < numOfFields; i++ {
		field := typeOfP.Field(i)
		fieldName := field.Name
		fieldValue := valOfP.Field(i)
		if fieldValue.Kind() == reflect.String && fieldValue.String() != "" {
			target[fieldName] = fieldValue.String()
		}
	}
	return nil
}
