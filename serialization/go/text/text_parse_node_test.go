package textserialization

import (
	assert "github.com/stretchr/testify/assert"
	testing "testing"
)

func TestTree(t *testing.T) {
	source := "\"stringValue\""
	sourceArray := []byte(source)
	parseNode, err := NewTextParseNode(sourceArray)
	if err != nil {
		t.Errorf("Error creating parse node: %s", err.Error())
	}
	someProp, err := parseNode.GetChildNode("someProp")
	assert.NotNil(t, err)
	assert.Nil(t, someProp)
    
	stringValue, err := parseNode.GetStringValue()
    assert.Nil(t, err)
    assert.NotNil(t, stringValue)
    assert.Equal(t, "stringValue", *stringValue)
}
