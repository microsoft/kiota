import pytest
import requests
import responses
from responses import matchers
from asyncmock import AsyncMock

from kiota.abstractions.authentication import AnonymousAuthenticationProvider
from kiota.abstractions.request_information import RequestInformation

from serialization_json.json_parse_node import JsonParseNode
from serialization_json.json_parse_node_factory import JsonParseNodeFactory
from serialization_json.json_serialization_writer_factory import JsonSerializationWriterFactory

from http_requests.requests_request_adapter import RequestsRequestAdapter

from .helpers import OfficeLocation, User
BASE_URL = "https://graph.microsoft.com"

@pytest.fixture
def auth_provider():
    return AnonymousAuthenticationProvider()           
    
@pytest.fixture
def parse_node_factory():
    return JsonParseNodeFactory()
    
@pytest.fixture
def serialization_writer_factory():
    return JsonSerializationWriterFactory()

@pytest.fixture
def request_info():
    return RequestInformation()

@pytest.fixture
def request_info_mock():
    return RequestInformation()
    
@pytest.fixture
@responses.activate
def simple_response(request_adapter):
    responses.add(
        responses.GET,
        url=BASE_URL,
        json={'error': 'not found'},
        status=404,
        match=[
            matchers.header_matcher({"Content-Type": "application/json"}, strict_match=True)
        ]
    )
    
    session = requests.Session()
    prepped = session.prepare_request(
        requests.Request(
            method="GET",
            url=BASE_URL,      
        )
    )
    prepped.headers = {"Content-Type": "application/json"}

    resp = session.send(prepped)
    return resp
    
@pytest.fixture
@responses.activate
def mock_user_response(mocker):
    responses.add(
        responses.GET,
        url=BASE_URL,
        json={
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
        },
        status=200,
        match=[
            matchers.header_matcher({"Content-Type": "application/json"}, strict_match=True)
        ]
    )
    
    session = requests.Session()
    prepped = session.prepare_request(
        requests.Request(
            method="GET",
            url=BASE_URL,      
        )
    )
    prepped.headers = {"Content-Type": "application/json"}

    resp = session.send(prepped)
    return resp

@pytest.fixture
@responses.activate
def mock_users_response(mocker):
    responses.add(
        responses.GET,
        url=BASE_URL,
        json=[{
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
            },],
        status=200,
        match=[
            matchers.header_matcher({"Content-Type": "application/json"}, strict_match=True)
        ]
    )
    
    session = requests.Session()
    prepped = session.prepare_request(
        requests.Request(
            method="GET",
            url=BASE_URL,      
        )
    )
    prepped.headers = {"Content-Type": "application/json"}

    resp = session.send(prepped)
    return resp

@pytest.fixture
@responses.activate
def mock_primitive_collection_response(mocker):
    responses.add(
        responses.GET,
        url=BASE_URL,
        json=[12.1, 12.2, 12.3, 12.4, 12.5],
        status=200,
        match=[
            matchers.header_matcher({"Content-Type": "application/json"}, strict_match=True)
        ]
    )
    
    session = requests.Session()
    prepped = session.prepare_request(
        requests.Request(
            method="GET",
            url=BASE_URL,      
        )
    )
    prepped.headers = {"Content-Type": "application/json"}

    resp = session.send(prepped)
    return resp

@pytest.fixture
@responses.activate
def mock_primitive_response(mocker):
    responses.add(
        responses.GET,
        url=BASE_URL,
        json=22.3,
        status=200,
        match=[
            matchers.header_matcher({"Content-Type": "application/json"}, strict_match=True)
        ]
    )
    
    session = requests.Session()
    prepped = session.prepare_request(
        requests.Request(
            method="GET",
            url=BASE_URL,      
        )
    )
    prepped.headers = {"Content-Type": "application/json"}

    resp = session.send(prepped)
    return resp

def test_create_requests_request_adapter(auth_provider, parse_node_factory, serialization_writer_factory):
    request_adapter =  RequestsRequestAdapter(auth_provider, parse_node_factory, serialization_writer_factory)
    assert request_adapter._authentication_provider is auth_provider
    assert request_adapter._parse_node_factory is parse_node_factory
    assert request_adapter._serialization_writer_factory is serialization_writer_factory
    
@pytest.fixture
def request_adapter(auth_provider, parse_node_factory, serialization_writer_factory):
    return RequestsRequestAdapter(auth_provider, parse_node_factory, serialization_writer_factory)

def test_get_serialization_writer_factory(request_adapter, serialization_writer_factory):
    assert request_adapter.get_serialization_writer_factory() is serialization_writer_factory

@responses.activate
def test_get_response_content_type(request_adapter, simple_response):
    content_type = request_adapter.get_response_content_type(simple_response)
    assert content_type == 'application/json'
    
def test_set_base_url_for_request_information(request_adapter, request_info):
    request_adapter.base_url = BASE_URL
    request_adapter.set_base_url_for_request_information(request_info)
    assert request_info.path_parameters["base_url"] == BASE_URL
    
def test_get_request_from_request_information(request_adapter, request_info):
    request_info.http_method = 'GET'
    request_info.set_url(BASE_URL)
    request_info.content = bytes('hello world', 'utf_8')
    req = request_adapter.get_request_from_request_information(request_info)
    assert isinstance(req, requests.PreparedRequest)
    
def test_enable_backing_store(request_adapter):
    request_adapter.enable_backing_store(None)
    assert request_adapter._parse_node_factory
    assert request_adapter._serialization_writer_factory
    
@pytest.mark.asyncio
async def test_get_root_parse_node(request_adapter, simple_response):
    assert simple_response.text == '{"error": "not found"}'
    assert simple_response.status_code == 404
    content_type = request_adapter.get_response_content_type(simple_response)
    assert content_type == 'application/json'
    
    root_node = await request_adapter.get_root_parse_node(simple_response)
    assert isinstance(root_node, JsonParseNode)

@pytest.mark.asyncio
@responses.activate
async def test_send_async(request_adapter, request_info, mock_user_response):
    request_adapter.get_http_response_message = AsyncMock(return_value = mock_user_response)
    resp = await request_adapter.get_http_response_message(request_info)
    assert resp.headers.get("content-type") == 'application/json'
    final_result = await request_adapter.send_async(request_info, User, None, {})
    assert isinstance(final_result, User)
    assert final_result.get_display_name() == "Diego Siciliani"
    assert final_result.get_office_location() == OfficeLocation.dunhill
    assert final_result.get_business_phones() == ["+1 205 555 0108"]
    assert final_result.get_age() == 21
    assert final_result.get_gpa() == 3.2
    assert final_result.get_is_active() == True
    assert final_result.get_mobile_phone() == None
    assert "@odata.context" in final_result.get_additional_data()
    
@pytest.mark.asyncio
@responses.activate
async def test_send_collection_async(request_adapter, request_info, mock_users_response):
    request_adapter.get_http_response_message = AsyncMock(return_value = mock_users_response)
    resp = await request_adapter.get_http_response_message(request_info)
    assert resp.headers.get("content-type") == 'application/json'
    final_result = await request_adapter.send_collection_async(request_info, User, None, {})
    assert isinstance(final_result[0], User)
    assert final_result[0].get_display_name() == "Adele Vance"
    assert final_result[0].get_office_location() == OfficeLocation.dunhill
    assert final_result[0].get_business_phones() == ["+1 425 555 0109"]
    assert final_result[0].get_age() == 21
    assert final_result[0].get_gpa() == 3.7
    assert final_result[0].get_is_active() == True
    assert final_result[0].get_mobile_phone() == None
    assert "@odata.context" in final_result[0].get_additional_data()
    
@pytest.mark.asyncio
@responses.activate
async def test_send_collection_of_primitive_async(request_adapter, request_info, mock_primitive_collection_response):
    request_adapter.get_http_response_message = AsyncMock(return_value = mock_primitive_collection_response)
    resp = await request_adapter.get_http_response_message(request_info)
    assert resp.headers.get("content-type") == 'application/json'
    final_result = await request_adapter.send_collection_of_primitive_async(request_info, int, None, {})
    assert final_result == [12.1, 12.2, 12.3, 12.4, 12.5]
    
@pytest.mark.asyncio
@responses.activate
async def test_send_primitive_async(request_adapter, request_info, mock_primitive_response):
    request_adapter.get_http_response_message = AsyncMock(return_value = mock_primitive_response)
    resp = await request_adapter.get_http_response_message(request_info)
    assert resp.headers.get("content-type") == 'application/json'
    final_result = await request_adapter.send_primitive_async(request_info, float, None, {})
    assert final_result == 22.3
