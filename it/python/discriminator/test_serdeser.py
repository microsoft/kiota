import pytest

from kiota_serialization_json.json_parse_node_factory import JsonParseNodeFactory
from kiota_serialization_json.json_serialization_writer_factory import JsonSerializationWriterFactory

from client.models.object import Object
from client.models.object1 import Object1
from client.models.object2 import Object2

@pytest.mark.asyncio
async def test_object1_deser():
    factory = JsonParseNodeFactory()
    root = factory.get_root_parse_node('application/json', '{"objectType": "obj1", "one": "foo"}'.encode('utf-8'))
    result = root.get_object_value(Object)
    assert hasattr(result, "object1")
    assert not hasattr(result, "object2")
    assert isinstance(result.object1, Object1)
    assert result.object1.object_type == "obj1"
    assert result.object1.one == "foo"

@pytest.mark.asyncio
async def test_object2_deser():
    factory = JsonParseNodeFactory()
    root = factory.get_root_parse_node('application/json', '{"objectType": "obj2", "two": "bar"}'.encode('utf-8'))
    result = root.get_object_value(Object)
    assert hasattr(result, "object2")
    assert not hasattr(result, "object1")
    assert isinstance(result.object2, Object2)
    assert result.object2.object_type == "obj2"
    assert result.object2.two == "bar"

@pytest.mark.asyncio
async def test_object1_ser():
    obj = Object()
    obj1 = Object1()
    obj1.object_type = "obj1"
    obj1.one = "foo"
    obj.object1 = obj1
    factory = JsonSerializationWriterFactory()
    writer = factory.get_serialization_writer('application/json')
    obj.serialize(writer)
    content = writer.get_serialized_content().decode('utf-8')
    assert content == '{"objectType": "obj1", "one": "foo"}'

@pytest.mark.asyncio
async def test_object2_ser():
    obj = Object()
    obj2 = Object2()
    obj2.object_type = "obj2"
    obj2.two = "bar"
    obj.object2 = obj2
    factory = JsonSerializationWriterFactory()
    writer = factory.get_serialization_writer('application/json')
    obj.serialize(writer)
    content = writer.get_serialized_content().decode('utf-8')
    assert content == '{"objectType": "obj2", "two": "bar"}'
