import ssl
from requests.adapters import HTTPAdapter
from typing import List
from urllib3 import PoolManager
from typing import List, Optional

from kiota.abstractions.request_option import RequestOption
        
class MiddlewarePipeline(HTTPAdapter):
    """MiddlewarePipeline, entry point of middleware
    The pipeline is implemented as a linked-list, read more about
    it here https://buffered.dev/middleware-python-requests/
    """
    def __init__(self):
        super().__init__()
        self._current_middleware = None
        self._first_middleware = None
        self.poolmanager = PoolManager(ssl_version=ssl.PROTOCOL_TLSv1_2)

    def add_middleware(self, middleware):
        if self._middleware_present():
            self._current_middleware.next = middleware
            self._current_middleware = middleware
        else:
            self._first_middleware = middleware
            self._current_middleware = self._first_middleware

    def send(self, request, **kwargs):

        if self._middleware_present():
            return self._first_middleware.send(request, **kwargs)
        # No middleware in pipeline, call superclass' send
        return super().send(request, **kwargs)

    def _middleware_present(self):
        return self._current_middleware


class BaseMiddleware(HTTPAdapter):
    """Base class for middleware. Handles moving a Request to the next middleware in the pipeline.
    If the current middleware is the last one in the pipeline, it makes a network request
    """
    def __init__(self):
        super().__init__()
        self.next = None

    def send(self, request, **kwargs):
        if self.next is None:
            return super().send(request, **kwargs)
        return self.next.send(request, **kwargs)