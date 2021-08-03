package serialization

type ParseNode interface {
	GetChildNode(index string) (ParseNode, error)
	GetCollectionOfObjectValues() []Parsable
	GetCollectionOfPrimitiveValues() []interface{}
	GetObjectValue() Parsable
}
