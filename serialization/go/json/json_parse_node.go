package jsonserialization

import (
	"bytes"
	"encoding/base64"
	"encoding/json"
	"errors"
	"io"
	"time"

	"github.com/google/uuid"
	absser "github.com/microsoft/kiota/abstractions/go/serialization"
)

type JsonParseNode struct {
	value interface{}
}

func NewJsonParseNode(content []byte) (*JsonParseNode, error) {
	if len(content) == 0 {
		return nil, errors.New("content is empty")
	}
	decoder := json.NewDecoder(bytes.NewReader(content))
	value, err := loadJsonTree(decoder)
	return value, err
}
func loadJsonTree(decoder *json.Decoder) (*JsonParseNode, error) {
	for {
		token, err := decoder.Token()
		if err == io.EOF {
			break
		}
		if err != nil {
			return nil, err
		}
		switch token.(type) {
		case json.Delim:
			switch token.(json.Delim) {
			case '{':
				v := make(map[string]*JsonParseNode)
				for decoder.More() {
					key, err := decoder.Token()
					if err != nil {
						return nil, err
					}
					keyStr, ok := key.(string)
					if !ok {
						return nil, errors.New("key is not a string")
					}
					childNode, err := loadJsonTree(decoder)
					if err != nil {
						return nil, err
					}
					v[keyStr] = childNode
				}
				decoder.Token() // skip the closing curly
				result := &JsonParseNode{value: v}
				return result, nil
			case '[':
				v := make([]*JsonParseNode, 0)
				for decoder.More() {
					childNode, err := loadJsonTree(decoder)
					if err != nil {
						return nil, err
					}
					v = append(v, childNode)
				}
				decoder.Token() // skip the closing bracket
				result := &JsonParseNode{value: v}
				return result, nil
			case ']':
			case '}':
			}
		case json.Number:
			number := token.(json.Number)
			i, err := number.Int64()
			c := &JsonParseNode{}
			if err == nil {
				c.SetValue(&i)
			} else {
				f, err := number.Float64()
				if err == nil {
					c.SetValue(&f)
				} else {
					return nil, err
				}
			}
			return c, nil
		case string:
			v := token.(string)
			c := &JsonParseNode{}
			c.SetValue(&v)
			return c, nil
		case bool:
			c := &JsonParseNode{}
			v := token.(bool)
			c.SetValue(&v)
			return c, nil
		case float64:
			c := &JsonParseNode{}
			v := token.(float64)
			c.SetValue(&v)
			return c, nil
		case float32:
			c := &JsonParseNode{}
			v := token.(float32)
			c.SetValue(&v)
			return c, nil
		case int32:
			c := &JsonParseNode{}
			v := token.(int32)
			c.SetValue(&v)
			return c, nil
		case int64:
			c := &JsonParseNode{}
			v := token.(int64)
			c.SetValue(&v)
			return c, nil
		case nil:
			return nil, nil
		default:
		}
	}
	return nil, nil
}
func (n *JsonParseNode) SetValue(value interface{}) {
	n.value = value
}
func (n *JsonParseNode) GetChildNode(index string) (absser.ParseNode, error) {
	if index == "" {
		return nil, errors.New("index is empty")
	}
	childNodes, ok := n.value.(map[string]*JsonParseNode)
	if !ok || len(childNodes) == 0 {
		return nil, errors.New("no child node available")
	}
	return childNodes[index], nil
}
func (n *JsonParseNode) GetObjectValue(ctor func() absser.Parsable) (absser.Parsable, error) {
	if ctor == nil {
		return nil, errors.New("constuctor is nil")
	}
	result := ctor()
	//TODO onbefore when implementing backing store
	properties, ok := n.value.(map[string]*JsonParseNode)
	if !ok {
		return nil, errors.New("value is not an object")
	}
	fields := result.GetFieldDeserializers()
	if len(properties) != 0 {
		for key, value := range properties {
			field := fields[key]
			if field == nil {
				result.GetAdditionalData()[key] = value.value
			} else {
				err := field(result, value)
				if err != nil {
					return nil, err
				}
			}
		}
	}
	//TODO on after when implmenting backing store
	return result, nil
}
func (n *JsonParseNode) GetCollectionOfObjectValues(ctor func() absser.Parsable) ([]absser.Parsable, error) {
	if n == nil || n.value == nil {
		return nil, nil
	}
	if ctor == nil {
		return nil, errors.New("ctor is nil")
	}
	nodes, ok := n.value.([]*JsonParseNode)
	if !ok {
		return nil, errors.New("value is not a collection")
	}
	result := make([]absser.Parsable, len(nodes))
	for i, v := range nodes {
		val, err := (*v).GetObjectValue(ctor)
		if err != nil {
			return nil, err
		}
		result[i] = val
	}
	return result, nil
}
func (n *JsonParseNode) GetCollectionOfPrimitiveValues(targetType string) ([]interface{}, error) {
	if n == nil || n.value == nil {
		return nil, nil
	}
	if targetType == "" {
		return nil, errors.New("targetType is empty")
	}
	nodes, ok := n.value.([]*JsonParseNode)
	if !ok {
		return nil, errors.New("value is not a collection")
	}
	result := make([]interface{}, len(nodes))
	for i, v := range nodes {
		val, err := v.getPrimitiveValue(targetType)
		if err != nil {
			return nil, err
		}
		result[i] = val
	}
	return result, nil
}
func (n *JsonParseNode) getPrimitiveValue(targetType string) (interface{}, error) {
	switch targetType {
	case "string":
		return n.GetStringValue()
	case "bool":
		return n.GetBoolValue()
	case "float32":
		return n.GetFloat32Value()
	case "float64":
		return n.GetFloat64Value()
	case "int32":
		return n.GetInt32Value()
	case "int64":
		return n.GetInt64Value()
	case "time":
		return n.GetTimeValue()
	case "uuid":
		return n.GetUUIDValue()
	case "base64":
		return n.GetByteArrayValue()
	default:
		return nil, errors.New("targetType is not supported")
	}
}
func (n *JsonParseNode) GetCollectionOfEnumValues(parser func(string) (interface{}, error)) ([]interface{}, error) {
	if n == nil || n.value == nil {
		return nil, nil
	}
	if parser == nil {
		return nil, errors.New("parser is nil")
	}
	nodes, ok := n.value.([]*JsonParseNode)
	if !ok {
		return nil, errors.New("value is not a collection")
	}
	result := make([]interface{}, len(nodes))
	for i, v := range nodes {
		val, err := v.GetEnumValue(parser)
		if err != nil {
			return nil, err
		}
		result[i] = val
	}
	return result, nil
}
func (n *JsonParseNode) GetStringValue() (*string, error) {
	if n == nil || n.value == nil {
		return nil, nil
	}
	return n.value.(*string), nil
}
func (n *JsonParseNode) GetBoolValue() (*bool, error) {
	if n == nil || n.value == nil {
		return nil, nil
	}
	return n.value.(*bool), nil
}
func (n *JsonParseNode) GetFloat32Value() (*float32, error) {
	v, err := n.GetFloat64Value()
	if err != nil {
		return nil, err
	}
	cast := float32(*v)
	return &cast, nil
}
func (n *JsonParseNode) GetFloat64Value() (*float64, error) {
	if n == nil || n.value == nil {
		return nil, nil
	}
	return n.value.(*float64), nil
}
func (n *JsonParseNode) GetInt32Value() (*int32, error) {
	v, err := n.GetFloat64Value()
	if err != nil {
		return nil, err
	}
	cast := int32(*v)
	return &cast, nil
}
func (n *JsonParseNode) GetInt64Value() (*int64, error) {
	v, err := n.GetFloat64Value()
	if err != nil {
		return nil, err
	}
	cast := int64(*v)
	return &cast, nil
}
func (n *JsonParseNode) GetTimeValue() (*time.Time, error) {
	v, err := n.GetStringValue()
	if err != nil {
		return nil, err
	}
	parsed, err := time.Parse(time.RFC3339, *v)
	return &parsed, err
}
func (n *JsonParseNode) GetUUIDValue() (*uuid.UUID, error) {
	v, err := n.GetStringValue()
	if err != nil {
		return nil, err
	}
	parsed, err := uuid.Parse(*v)
	return &parsed, err
}
func (n *JsonParseNode) GetEnumValue(parser func(string) (interface{}, error)) (interface{}, error) {
	if parser == nil {
		return nil, errors.New("parser is nil")
	}
	s, err := n.GetStringValue()
	if err != nil {
		return nil, err
	}
	return parser(*s)
}
func (n *JsonParseNode) GetByteArrayValue() ([]byte, error) {
	s, err := n.GetStringValue()
	if err != nil {
		return nil, err
	}
	return base64.StdEncoding.DecodeString(*s)
}
