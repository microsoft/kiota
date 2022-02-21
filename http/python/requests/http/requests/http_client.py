
import requests
from typing import Dict, List
from kiota.abstractions.request_option import RequestOption

from .kiota_client_factory import get_default_middlewares, get_default_request_settings
from .middleware import Middleware

class HttpClient:
    """Default requests client with options and a middleware pipleline for requests execution. 
    """
    def __init__(self, middleware: List[Middleware], default_request_settings: Dict) -> None:
        self
 