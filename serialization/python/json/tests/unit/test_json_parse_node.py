import json
from datetime import date, datetime, time, timedelta
from uuid import UUID

import pytest

from serialization.json_parse_node import JsonParseNode

from ..helpers import OfficeLocation, User


@pytest.fixture
def sample_user_json():

    user_json = json.dumps(
        {
            "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#users/$entity",
            "businessPhones": ["+1 205 555 0108"],
            "displayName": "Diego Siciliani",
            "mobilePhone": None,
            "officeLocation": "dunhill",
            "updatedAt": "2021 -07-29T03:07:25Z",
            "birthday": "2000-09-04",
            "isActive": True,
            "age": 21,
            "gpa": 3.2,
            "id": "8f841f30-e6e3-439a-a812-ebd369559c36"
        }
    )
    return user_json


@pytest.fixture
def sample_users_json():
    users_json = json.dumps(
        [
            {
                "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#users/$entity",
                "businessPhones": ["+1 425 555 0109"],
                "displayName": "Adele Vance",
                "mobilePhone": None,
                "officeLocation": "dunhill",
                "updatedAt": "2017 -07-29T03:07:25Z",
                "birthday": "2000-09-04",
                "isActive": True,
                "age": 21,
                "gpa": 3.7,
                "id": "76cabd60-f9aa-4d23-8958-64f5539b826a"
            },
            {
                "businessPhones": ["425-555-0100"],
                "displayName": "MOD Administrator",
                "mobilePhone": None,
                "officeLocation": "oval",
                "updatedAt": "2020 -07-29T03:07:25Z",
                "birthday": "1990-09-04",
                "isActive": True,
                "age": 32,
                "gpa": 3.9,
                "id": "f58411c7-ae78-4d3c-bb0d-3f24d948de41"
            },
        ]
    )
    return users_json


def test_get_string_value():
    parse_node = JsonParseNode("Diego Siciliani")
    result = parse_node.get_string_value()
    assert result == "Diego Siciliani"


def test_get_int_value():
    parse_node = JsonParseNode("1454")
    result = parse_node.get_int_value()
    assert result == 1454


def test_get_boolean_value():
    parse_node = JsonParseNode(False)
    result = parse_node.get_boolean_value()
    assert result == False


def test_get_float_value():
    parse_node = JsonParseNode(44.6)
    result = parse_node.get_float_value()
    assert result == 44.6


def test_get_uuid_value():
    parse_node = JsonParseNode("f58411c7-ae78-4d3c-bb0d-3f24d948de41")
    result = parse_node.get_uuid_value()
    assert result == UUID("f58411c7-ae78-4d3c-bb0d-3f24d948de41")


def test_get_datetime_offset_value():
    parse_node = JsonParseNode('2022-01-27T12:59:45.596117')
    result = parse_node.get_datetime_offset_value()
    assert isinstance(result, datetime)


def test_get_date_value():
    parse_node = JsonParseNode('2015-04-20T11:50:51Z')
    result = parse_node.get_date_value()
    assert isinstance(result, date)
    assert str(result) == '2015-04-20'


def test_get_time_value():
    parse_node = JsonParseNode('2022-01-27T12:59:45.596117')
    result = parse_node.get_time_value()
    assert isinstance(result, time)
    assert str(result) == '12:59:45.596117'


def test_get_timedelta_value():
    parse_node = JsonParseNode('2022-01-27T12:59:45.596117')
    result = parse_node.get_timedelta_value()
    assert isinstance(result, timedelta)
    assert str(result) == '12:59:45'


def test_get_collection_of_primitive_values():
    parse_node = JsonParseNode([12.1, 12.2, 12.3, 12.4, 12.5])
    result = parse_node.get_collection_of_primitive_values()
    assert result == [12.1, 12.2, 12.3, 12.4, 12.5]


def test_get_byte_array_value():
    parse_node = JsonParseNode('U2Ftd2VsIGlzIHRoZSBiZXN0')
    result = parse_node.get_byte_array_value()
    assert isinstance(result, bytes)


def test_get_collection_of_enum_values():
    parse_node = JsonParseNode("dunhill,oval")
    result = parse_node.get_collection_of_enum_values(OfficeLocation)
    assert isinstance(result, list)
    assert result == [OfficeLocation.dunhill, OfficeLocation.oval]


def test_get_enum_value():
    parse_node = JsonParseNode("dunhill")
    result = parse_node.get_enum_value(OfficeLocation)
    assert isinstance(result, OfficeLocation)
    assert result == OfficeLocation.dunhill


def test_get_object_value(sample_user_json):
    parse_node = JsonParseNode(sample_user_json)
    result = parse_node.get_object_value(User)
    assert isinstance(result, User)
    assert result.get_id() == UUID("8f841f30-e6e3-439a-a812-ebd369559c36")
    assert result.get_display_name() == "Diego Siciliani"
    assert result.get_office_location() == OfficeLocation.dunhill
    assert isinstance(result.get_updated_at(), datetime)
    assert isinstance(result.get_birthday(), date)
    assert result.get_business_phones() == ["+1 205 555 0108"]
    assert result.get_age() == 21
    assert result.get_gpa() == 3.2
    assert result.get_is_active() == True
    assert result.get_mobile_phone() == None
    assert "@odata.context" in result.get_additional_data()


def test_get_collection_of_object_values(sample_users_json):
    parse_node = JsonParseNode(sample_users_json)
    result = parse_node.get_collection_of_object_values(User)
    assert isinstance(result[0], User)
    assert result[0].get_id() == UUID("76cabd60-f9aa-4d23-8958-64f5539b826a")
    assert result[0].get_display_name() == "Adele Vance"
    assert result[0].get_office_location() == OfficeLocation.dunhill
    assert isinstance(result[0].get_updated_at(), datetime)
    assert isinstance(result[0].get_birthday(), date)
    assert result[0].get_business_phones() == ["+1 425 555 0109"]
    assert result[0].get_age() == 21
    assert result[0].get_gpa() == 3.7
    assert result[0].get_is_active() == True
    assert result[0].get_mobile_phone() == None
    assert "@odata.context" in result[0].get_additional_data()
