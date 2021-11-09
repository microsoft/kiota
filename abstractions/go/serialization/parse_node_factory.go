package serialization

// Defines the contract for a factory that creates parse nodes.
type ParseNodeFactory interface {
	// Returns the content type this factory's parse nodes can deserialize.
	GetValidContentType() (string, error)
	// Get the ParseNode instance that is the root of the content
	// Parameters:
	//   contentType: The content type of the content to be parsed
	//   content: The content to be parsed
	// Returns:
	// - A ParseNode instance that is the root of the content
	// - An error if the content type is not supported
	GetRootParseNode(contentType string, content []byte) (ParseNode, error)
}
