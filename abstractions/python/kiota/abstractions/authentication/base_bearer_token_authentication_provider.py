from abc import abstractmethod
from typing import Union

from ..request_information import RequestInformation
from .authentication_provider import AuthenticationProvider

AUTHORIZATION_HEADER = "Authorization"


class BaseBearerAuthenticationProvider(AuthenticationProvider):
    """Provides a base class implementing AuthenticationProvider for Bearer token scheme.

    Args:
        AuthenticationProvider (ABC):The abstract base class that this class implements
    """
    async def authenticate_request(self, request: RequestInformation) -> None:
        """Authenticates the provided RequestInformation instance using the provided authorization token

        Args:
            request (RequestInformation): Request information object
        """
        if AUTHORIZATION_HEADER in request.headers:
            token = await self.get_authorization_token(request)
            if not token:
                raise Exception("Failed to get an authorization token")
            
            request.headers.update({f'{AUTHORIZATION_HEADER}': f'Bearer {token}'})
    
    @abstractmethod
    def get_authorization_token(self, request: RequestInformation) -> Union[str, None]:
        """Gets the authorization token for the given request.

        Args:
            request (RequestInformation): An instance of RequestInformation from which to obtain the token
        """
        raise NotImplementedError
