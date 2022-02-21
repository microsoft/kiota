import json

import pytest
from kiota.abstractions.serialization import ParseNodeFactory

from serialization_json.json_parse_node import JsonParseNode
from serialization_json.json_parse_node_factory import JsonParseNodeFactory


@pytest.fixture
def sample_json_string():
    return '{"name":"Tesla", "age":2, "city":"New York"}'


def test_get_root_parse_node(sample_json_string):
    factory = JsonParseNodeFactory()
    sample_json_string_bytes = sample_json_string.encode('utf-8')
    root = factory.get_root_parse_node('application/json', sample_json_string_bytes)
    assert isinstance(root, JsonParseNode)


def test_get_root_parse_node_no_content_type(sample_json_string):
    with pytest.raises(Exception) as e_info:
        factory = JsonParseNodeFactory()
        sample_json_string_bytes = sample_json_string.encode('utf-8')
        root = factory.get_root_parse_node('', sample_json_string_bytes)


def test_get_root_parse_node_unsupported_content_type(sample_json_string):
    with pytest.raises(Exception) as e_info:
        factory = JsonParseNodeFactory()
        sample_json_string_bytes = sample_json_string.encode('utf-8')
        root = factory.get_root_parse_node('application/xml', sample_json_string_bytes)


def test_get_root_parse_node_invalid_json():
    with pytest.raises(json.JSONDecodeError) as e_info:
        factory = JsonParseNodeFactory()
        sample_string_bytes = 'Not Json'.encode('utf-8')
        root = factory.get_root_parse_node('application/json', sample_string_bytes)


def test_get_root_parse_node_empty_json():
    with pytest.raises(TypeError) as e_info:
        factory = JsonParseNodeFactory()
        sample_string_bytes = ''.encode('utf-8')
        root = factory.get_root_parse_node('application/json', sample_string_bytes)


def test_get_valid_content_type():
    factory = JsonParseNodeFactory()
    content_type = factory.get_valid_content_type()
    assert content_type == 'application/json'
