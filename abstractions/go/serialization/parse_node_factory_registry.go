package serialization

import "errors"

// implements Parse Node Factory
type ParseNodeFactoryRegistry struct {
	ContentTypeAssociatedFactories map[string]ParseNodeFactory
}

var DefaultParseNodeFactoryInstance = &ParseNodeFactoryRegistry{
	ContentTypeAssociatedFactories: make(map[string]ParseNodeFactory),
}

func (m *ParseNodeFactoryRegistry) GetValidContentType() (string, error) {
	return "", errors.New("the registry supports multiple content types. Get the registered factory instead")
}

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
