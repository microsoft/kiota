"""
Kiota http request adapter implementation with requests
"""
from ._version import VERSION

__version__ = VERSION

from .kiota_client import KiotaClient
from .kiota_client_factory import KiotaClientFactory
