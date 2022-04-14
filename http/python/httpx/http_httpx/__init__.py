"""
Kiota http request adapter implementation for httpx library
"""
from ._version import VERSION

__version__ = VERSION

from .kiota_client import KiotaClient
from .kiota_client_factory import KiotaClientFactory
