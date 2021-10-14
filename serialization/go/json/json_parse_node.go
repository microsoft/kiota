package jsonserialization

import (
	"encoding/base64"
	"errors"
	"time"

	"github.com/google/uuid"
	absser "github.com/microsoft/kiota/abstractions/go/serialization"
)

type JsonParseNode struct {
	childNodes map[string]*JsonParseNode
	value      interface{}
}

func NewJsonParseNode(content []byte) (*JsonParseNode, error) {
	value := JsonParseNode{
		childNodes: make(map[string]*JsonParseNode),
	}
	if content != nil {
		//TODO build the tree node putting properties in child nodes, arrays and scalars in value
		// https://pkg.go.dev/encoding/json#Decoder.Token
	}
	return &value, nil
}
func (n *JsonParseNode) SetValue(value interface{}) {
	n.value = value
}
func (n *JsonParseNode) GetChildNode(index string) (absser.ParseNode, error) {
	if index == "" {
		return nil, errors.New("index is empty")
	}
	if len(n.childNodes) == 0 {
		return nil, errors.New("no child node available")
	}
	return n.childNodes[index], nil
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
