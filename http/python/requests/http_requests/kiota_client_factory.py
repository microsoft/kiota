import functools
import requests

from typing import Dict, List

from .middleware import BaseMiddleware, MiddlewarePipeline, RetryHandler, TelemetryHandler

class KiotaClientFactory:
    
    def create_with_default_middleware(self) -> requests.Session:
        """Constructs native HTTP Client(requests.Session) instances configured with a default 
        pipeline of middleware.

        Returns:
            Session: An instance of the requests session object
        """
        session = requests.Session()
        self._set_default_timeout(session)
        self._register_default_middleware(session)
        return self.session
    
    def _set_default_timeout(self, session: requests.Session) -> None:
        """Helper method to set a default timeout for the session
        Reference: https://github.com/psf/requests/issues/2011
        """
        self.session.request = functools.partial(self.session.request, timeout=self.timeout)

    def _register_default_middleware(self) -> None:
        """
        Helper method that constructs a middleware_pipeline with the specified middleware
        """
        middleware_pipeline = MiddlewarePipeline()
        middlewares = [
            RetryHandler(),
        ]
            
        for middleware in middlewares:
            middleware_pipeline.add_middleware(middleware)
        self.session.mount('https://', middleware_pipeline)
        
        
    

