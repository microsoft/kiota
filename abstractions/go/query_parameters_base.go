package abstractions

import (
	"errors"
	"reflect"
)

type QueryParametersBase struct {
}

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
