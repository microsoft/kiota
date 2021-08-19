package serialization

type ParseNodeFactory interface {
	GetValidContentType() (string, error)
	GetRootParseNode(contentType string, content []byte) (ParseNode, error)
}
