import pytest
from kiota_abstractions.headers_collection import HeadersCollection
from kiota_abstractions.authentication.anonymous_authentication_provider import (
    AnonymousAuthenticationProvider,
)
from kiota_http.httpx_request_adapter import HttpxRequestAdapter

from client.api_client import ApiClient
from client.models.new_todo import NewTodo
from client.todos.todos_request_builder import TodosRequestBuilder

@pytest.mark.asyncio
async def test_basic_upload_download():
    auth_provider = AnonymousAuthenticationProvider()
    request_adapter = HttpxRequestAdapter(auth_provider)
    request_adapter.base_url = 'http://127.0.0.1:1080'
    client = ApiClient(request_adapter)
    
    myHeaders = HeadersCollection()
    myHeaders.add("My-Extra-Header", "hello")
    config = TodosRequestBuilder.TodosRequestBuilderPostRequestConfiguration(
        headers = myHeaders
    )
    
    await client.todos.post(NewTodo(), config)
