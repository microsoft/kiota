from typing import Union

from ..request_information import RequestInformation
from .authentication_provider import AuthenticationProvider


class BaseBearerAuthenticationProvider(AuthenticationProvider):
    """Provides a base class implementing AuthenticationProvider for Bearer token scheme.

    Args:
        AuthenticationProvider (ABC):The abstract base class that this class implements
    """
    AUTHORIZATION_HEADER = "Authorization"

    async def authenticate_request(self, request: RequestInformation) -> None:
        """Authenticates the provided RequestInformation instance using the provided
        authorization token

        Args:
            request (RequestInformation): Request information object
        """
        if not request:
            raise Exception("Request cannot be null")
        if self.AUTHORIZATION_HEADER in request.headers:
            token = await self.get_authorization_token(request)
            if not token:
                raise Exception("Failed to get an authorization token")

            request.headers.update({f'{self.AUTHORIZATION_HEADER}': f'Bearer {token}'})

    async def get_authorization_token(self, request: RequestInformation) -> str:
        """Gets the authorization token for the given request.

        Args:
            request (RequestInformation): An instance of RequestInformation from which to obtain
            the token

        Returns:
            str: Access token to use for the request
        """
        raise NotImplementedError

