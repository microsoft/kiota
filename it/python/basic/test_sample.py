import pytest
from kiota_abstractions.authentication.anonymous_authentication_provider import (
    AnonymousAuthenticationProvider,
)
from kiota_http.httpx_request_adapter import HttpxRequestAdapter

from client.api_client import ApiClient
from client.models.error import Error

@pytest.mark.asyncio
async def test_basic_upload_download():
    auth_provider = AnonymousAuthenticationProvider()
    request_adapter = HttpxRequestAdapter(auth_provider)
    request_adapter.base_url = 'http://127.0.0.1:1080'
    client = ApiClient(request_adapter)

    with pytest.raises(Error) as execinfo:
        await client.api.v1.topics.get()
    
    assert execinfo.value.id == "my-sample-id"
    assert execinfo.value.code == 123
