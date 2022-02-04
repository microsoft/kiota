"""
Authentication provider for Kiota using Azure Identity
"""
from ._version import VERSION
from .azure_identity_access_token_provider import AzureIdentityAccessTokenProvider
from .azure_identity_authentication_provider import AzureIdentityAuthenticationProvider

__version__ = VERSION
