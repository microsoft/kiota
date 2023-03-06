from azure.identity.aio import DefaultAzureCredential
from kiota_authentication_azure.azure_identity_authentication_provider import AzureIdentityAuthenticationProvider
from kiota_http.httpx_request_adapter import HttpxRequestAdapter
from .client.api_client import ApiClient

credential=DefaultAzureCredential()
auth_provider = AzureIdentityAuthenticationProvider(credential)
adapter = HttpxRequestAdapter(auth_provider)
client = ApiClient(adapter)
