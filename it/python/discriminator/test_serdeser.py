import pytest

from kiota_serialization_json.json_parse_node_factory import JsonParseNodeFactory
from kiota_serialization_json.json_serialization_writer_factory import JsonSerializationWriterFactory

from client.models.component import Component
from client.models.component1 import Component1
from client.models.component2 import Component2

@pytest.mark.asyncio
async def test_component1_deser():
    factory = JsonParseNodeFactory()
    root = factory.get_root_parse_node('application/json', '{"objectType": "obj1", "one": "foo"}'.encode('utf-8'))
    result = root.get_object_value(Component)
    assert hasattr(result, "component1")
    assert not hasattr(result, "component2")
    assert isinstance(result.component1, Component1)
    assert result.component1.object_type == "obj1"
    assert result.component1.one == "foo"

@pytest.mark.asyncio
async def test_component2_deser():
    factory = JsonParseNodeFactory()
    root = factory.get_root_parse_node('application/json', '{"objectType": "obj2", "two": "bar"}'.encode('utf-8'))
    result = root.get_object_value(Component)
    assert hasattr(result, "component2")
    assert not hasattr(result, "component1")
    assert isinstance(result.component2, Component2)
    assert result.component2.object_type == "obj2"
    assert result.component2.two == "bar"

@pytest.mark.asyncio
async def test_component1_ser():
    obj = Component()
    obj1 = Component1()
    obj1.object_type = "obj1"
    obj1.one = "foo"
    obj.component1 = obj1
    factory = JsonSerializationWriterFactory()
    writer = factory.get_serialization_writer('application/json')
    obj.serialize(writer)
    content = writer.get_serialized_content().decode('utf-8')
    assert content == '{"objectType": "obj1", "one": "foo"}'

@pytest.mark.asyncio
async def test_component2_ser():
    obj = Component()
    obj2 = Component2()
    obj2.object_type = "obj2"
    obj2.two = "bar"
    obj.component2 = obj2
    factory = JsonSerializationWriterFactory()
    writer = factory.get_serialization_writer('application/json')
    obj.serialize(writer)
    content = writer.get_serialized_content().decode('utf-8')
    assert content == '{"objectType": "obj2", "two": "bar"}'
