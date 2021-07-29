package serialization

type ParseNode interface {
	getChildNode(index string) (ParseNode, error)
	getCollectionOfObjectValues() []Parsable
	getCollectionOfPrimitiveValues() []interface{}
	getObjectValue() Parsable
}
