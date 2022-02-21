from typing import Dict, List
from .middleware import Middleware

def get_default_middlewares() -> List[Middleware]:
    """Gets the default middlewares in use for the client.

    Returns:
        List[Middleware]: the default middlewares.
    """
    return []

def get_default_request_setting() -> Dict:
    """Gets the default request settings to be used for the client.

    Returns:
        RequestInit: the default request settings.
    """
    return {}