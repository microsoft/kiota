import functools

import requests

from .middleware import MiddlewarePipeline, RetryHandler


class KiotaClientFactory:
    DEFAULT_CONNECTION_TIMEOUT: int = 30
    DEFAULT_REQUEST_TIMEOUT: int = 100

    def create_with_default_middleware(self) -> requests.Session:
        """Constructs native HTTP Client(requests.Session) instances configured with a default
        pipeline of middleware.

        Returns:
            Session: An instance of the requests session object
        """
        session = requests.Session()
        self._set_default_timeout(session)
        return self._register_default_middleware(session)

    def _set_default_timeout(self, session: requests.Session) -> None:
        """Helper method to set a default timeout for the session
        Reference: https://github.com/psf/requests/issues/2011
        """
        session.request = functools.partial( #type:ignore
            session.request,
            timeout=(self.DEFAULT_CONNECTION_TIMEOUT, self.DEFAULT_REQUEST_TIMEOUT)
        )

    def _register_default_middleware(self, session: requests.Session) -> requests.Session:
        """
        Helper method that constructs a middleware_pipeline with the specified middleware
        """
        middleware_pipeline = MiddlewarePipeline()
        middlewares = [
            RetryHandler(),
        ]

        for middleware in middlewares:
            middleware_pipeline.add_middleware(middleware)

        session.mount('https://', middleware_pipeline)
        return session
