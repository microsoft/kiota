import pytest
from requests import Session
from requests.adapters import HTTPAdapter

from http_requests import KiotaClientFactory

def test_create_with_default_middleware():
    """Test creation of HTTP Client using default middleware"""
    client = KiotaClientFactory().create_with_default_middleware()
    middleware = client.get_adapter('https://')

    assert isinstance(middleware, HTTPAdapter)
    
def test_register_middleware():
    client = KiotaClientFactory()._register_default_middleware(Session())

    assert isinstance(client.get_adapter('https://'), HTTPAdapter)