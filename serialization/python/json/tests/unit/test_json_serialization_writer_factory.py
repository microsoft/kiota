import pytest
from kiota.abstractions.serialization import SerializationWriterFactory

from serialization.json_serialization_writer import JsonSerializationWriter
from serialization.json_serialization_writer_factory import JsonSerializationWriterFactory


def test_get_serialization_writer():
    factory = JsonSerializationWriterFactory()
    writer = factory.get_serialization_writer('application/json')
    assert isinstance(writer, JsonSerializationWriter)


def test_get_serialization_writer_no_content_type():
    with pytest.raises(TypeError) as e_info:
        factory = JsonSerializationWriterFactory()
        writer = factory.get_serialization_writer('')


def test_get_serialization_writer_unsupported_content_type():
    with pytest.raises(Exception) as e_info:
        factory = JsonSerializationWriterFactory
        writer = factory.get_root_parse_node('application/xml')


def test_get_valid_content_type():
    factory = JsonSerializationWriterFactory()
    content_type = factory.get_valid_content_type()
    assert content_type == 'application/json'
