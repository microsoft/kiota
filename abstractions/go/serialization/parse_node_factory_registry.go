package serialization

import "errors"

// This factory holds a list of all the registered factories for the various types of nodes.
type ParseNodeFactoryRegistry struct {
	ContentTypeAssociatedFactories map[string]ParseNodeFactory
}

// Default singleton instance of the registry to be used when registring new factories that should be available by default.
var DefaultParseNodeFactoryInstance = &ParseNodeFactoryRegistry{
	ContentTypeAssociatedFactories: make(map[string]ParseNodeFactory),
}

// Gets the valid content type for the ParseNodeFactoryRegistry
func (m *ParseNodeFactoryRegistry) GetValidContentType() (string, error) {
	return "", errors.New("the registry supports multiple content types. Get the registered factory instead")
}

// Get the ParseNode instance that is the root of the content
// Parameters:
//   contentType: The content type of the content to be parsed
//   content: The content to be parsed
// Returns:
// - A ParseNode instance that is the root of the content
// - An error if the content type is not supported
func (m *ParseNodeFactoryRegistry) GetRootParseNode(contentType string, content []byte) (ParseNode, error) {
	if contentType == "" {
		return nil, errors.New("contentType is required")
	}
	if content == nil {
		return nil, errors.New("content is required")
	}
	factory, ok := m.ContentTypeAssociatedFactories[contentType]
	if !ok {
		return nil, errors.New("content type " + contentType + " does not have a factory registered to be parsed")
	} else {
		return factory.GetRootParseNode(contentType, content)
	}
}
