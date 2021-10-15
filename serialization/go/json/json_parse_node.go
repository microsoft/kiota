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
	value := &JsonParseNode{}
	if content != nil {
		decoder := json.NewDecoder(bytes.NewReader(content))
		err := value.loadJsonTree(decoder)
		if err != nil {
			return nil, err
		}
	}
	return value, nil
}
func (c *JsonParseNode) loadJsonTree(decoder *json.Decoder) error {
	for {
		token, err := decoder.Token()
		if err == io.EOF {
			break
		}
		if err != nil {
			return err
		}
		switch token.(type) {
		case json.Delim:
			switch token {
			case '{':
				result := make(map[string]*JsonParseNode)
				for decoder.More() {
					key, err := decoder.Token()
					if err != nil {
						return err
					}
					keyStr, ok := key.(string)
					if !ok {
						return errors.New("key is not a string")
					}
					childNode := &JsonParseNode{}
					err = childNode.loadJsonTree(decoder)
					if err != nil {
						return err
					}
					result[keyStr] = childNode
				}
				c.SetValue(result)
			case '[':
				result := make([]JsonParseNode, 0)
				for decoder.More() {
					node := JsonParseNode{}
					err := node.loadJsonTree(decoder)
					if err != nil {
						return err
					}
					result = append(result, node)
				}
				c.SetValue(result)
			case ']':
			case '}':
			}
		case json.Number:
			number := token.(json.Number)
			i, err := number.Int64()
			if err == nil {
				c.SetValue(&i)
			} else {
				f, err := number.Float64()
				if err == nil {
					c.SetValue(&f)
				} else {
					return err
				}
			}
		case string:
			c.SetValue(token.(*string))
		case bool:
			c.SetValue(token.(*bool))
		case nil:
		default:
		}
	}
	return nil
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
	//TODO onbefore when implementing backing store
	//TODO assign additional properties
	//TODO on after when implmenting backing store
	return nil, nil
}
func (n *JsonParseNode) GetCollectionOfObjectValues(ctor func() absser.Parsable) ([]absser.Parsable, error) {
	if n.value == nil {
		return nil, errors.New("value is nil")
	}
	if ctor == nil {
		return nil, errors.New("ctor is nil")
	}
	nodes, ok := n.value.([]absser.ParseNode)
	if !ok {
		return nil, errors.New("value is not a collection")
	}
	result := make([]absser.Parsable, len(nodes))
	for i, v := range nodes {
		val, err := v.GetObjectValue(ctor)
		if err != nil {
			return nil, err
		}
		result[i] = val
	}
	return result, nil
}
func (n *JsonParseNode) GetCollectionOfPrimitiveValues(targetType string) ([]interface{}, error) {
	if n.value == nil {
		return nil, errors.New("value is nil")
	}
	if targetType == "" {
		return nil, errors.New("targetType is empty")
	}
	nodes, ok := n.value.([]JsonParseNode)
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
	if n.value == nil {
		return nil, errors.New("value is nil")
	}
	if parser == nil {
		return nil, errors.New("parser is nil")
	}
	nodes, ok := n.value.([]absser.ParseNode)
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
	if n.value == nil {
		return nil, errors.New("value is nil")
	}
	return n.value.(*string), nil
}
func (n *JsonParseNode) GetBoolValue() (*bool, error) {
	if n.value == nil {
		return nil, errors.New("value is nil")
	}
	return n.value.(*bool), nil
}
func (n *JsonParseNode) GetFloat32Value() (*float32, error) {
	if n.value == nil {
		return nil, errors.New("value is nil")
	}
	return n.value.(*float32), nil
}
func (n *JsonParseNode) GetFloat64Value() (*float64, error) {
	if n.value == nil {
		return nil, errors.New("value is nil")
	}
	return n.value.(*float64), nil
}
func (n *JsonParseNode) GetInt32Value() (*int32, error) {
	if n.value == nil {
		return nil, errors.New("value is nil")
	}
	return n.value.(*int32), nil
}
func (n *JsonParseNode) GetInt64Value() (*int64, error) {
	if n.value == nil {
		return nil, errors.New("value is nil")
	}
	return n.value.(*int64), nil
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
