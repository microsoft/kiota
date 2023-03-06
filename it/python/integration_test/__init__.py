from .client.api_client import ApiClient
from azure.identity.aio import DefaultAzureCredential
from kiota_authentication_azure.azure_identity_authentication_provider import AzureIdentityAuthenticationProvider
from kiota_http import HttpxRequestAdapter

credential=DefaultAzureCredential()
auth_provider = AzureIdentityAuthenticationProvider(credential)
adapter = HttpxRequestAdapter(auth_provider)
client = ApiClient(adapter)
