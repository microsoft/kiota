import base64, json
import pytest

from datetime import date, datetime, time, timedelta
from enum import Enum
from io import BytesIO
from typing import Any, Callable, Dict, List, Optional, TypeVar
from uuid import UUID
from dateutil import parser

from kiota.abstractions.serialization import Parsable, SerializationWriter
from serialization.json_serialization_writer import JsonSerializationWriter

from ..helpers import OfficeLocation, User

@pytest.fixture
def user_1():
    user = User()
    user.set_age(31)
    user.set_is_active(True)
    user.set_display_name("Jane Doe")
    return user

@pytest.fixture
def user_2():
    user = User()
    user.set_age(32)
    user.set_is_active(False)
    user.set_display_name("John Doe")
    return user

def test_write_string_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_string_value("displayName", "Adele Vance")
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"displayName": "Adele Vance"}'
    

def test_write_boolean_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_boolean_value("isActive", True)
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"isActive": true}'    

def test_write_int_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_int_value("age", 21)
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"age": 21}'
    

def test_write_float_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_float_value("gpa", 3.2)
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"gpa": 3.2}'
    
def test_write_uuid_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_uuid_value("id", UUID("8f841f30-e6e3-439a-a812-ebd369559c36"))
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"id": "8f841f30-e6e3-439a-a812-ebd369559c36"}'

def test_write_datetime_offset_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_datetime_offset_value("updatedAt", parser.parse('2022-01-27T12:59:45.596117'))
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"updatedAt": "2022-01-27T12:59:45.596117"}'

def test_write_timedelta_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_timedelta_value("diff", parser.parse('2022-01-27T12:59:45.596117') - parser.parse('2022-01-27T10:59:45.596117'))
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"diff": "2:00:00"}'
    
def test_write_date_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_date_value("birthday", parser.parse("2000-09-04").date())
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"birthday": "2000-09-04"}'

def test_write_time_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_time_value("time", parser.parse('2022-01-27T12:59:45.596117').time())
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"time": "12:59:45.596117"}'

# def test_write_collection_of_primitive_values():
#     json_serialization_writer = JsonSerializationWriter()
#     json_serialization_writer.write_time_value("businessPhones", ["+1 412 555 0109", 1])
#     stream = json_serialization_writer.get_serialized_content()
#     content = stream.read()
#     content_string = content.decode('utf-8')
#     assert content_string == '{"businessPhones": ["+1 412 555 0109", 1]}'

def test_write_collection_of_object_values(user_1, user_2):
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_collection_of_object_values("users", [user_1,user_2])
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"users": [{"id": null, "display_name": "Jane Doe", "office_location": null, "updated_at": null, "birthday": null, "business_phones": null, "mobile_phone": null, "is_active": true, "age": 31, "gpa": null}, {"id": null, "display_name": "John Doe", "office_location": null, "updated_at": null, "birthday": null, "business_phones": null, "mobile_phone": null, "is_active": null, "age": 32, "gpa": null}]}'
    
def test_write_collection_of_enum_values():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_collection_of_enum_values("officeLocation", [OfficeLocation.dunhill,OfficeLocation.oval])
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"officeLocation": "dunhill,oval"}'
    
def test_write_object_value(user_1):
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_object_value("user1", user_1)
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"user1": {"id": null, "display_name": "Jane Doe", "office_location": null, "updated_at": null, "birthday": null, "business_phones": null, "mobile_phone": null, "is_active": true, "age": 31, "gpa": null}}'
    

def test_write_enum_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_enum_value("officeLocation", OfficeLocation.dunhill)
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"officeLocation": "dunhill"}'

def test_write_null_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_null_value("mobilePhone")
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"mobilePhone": null}'
    
def test_write_additional_data_value():
    json_serialization_writer = JsonSerializationWriter()
    json_serialization_writer.write_additional_data_value({"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#users/$entity", 
        "businessPhones": [
                "+1 205 555 0108"
            ],})
    stream = json_serialization_writer.get_serialized_content()
    content = stream.read()
    content_string = content.decode('utf-8')
    assert content_string == '{"@odata.context": "https://graph.microsoft.com/v1.0/$metadata#users/$entity", "businessPhones": ["+1 205 555 0108"]}'

    