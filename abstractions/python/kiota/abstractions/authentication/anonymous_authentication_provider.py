from ..request_information import RequestInformation
from .authentication_provider import AuthenticationProvider


class AnonymousAuthenticationProvider(AuthenticationProvider):
    """This authentication provider does not perform any authentication

    Args:
        AuthenticationProvider (ABC): The abstract base class that this class implements
    """

    async def authenticate_request(self, request: RequestInformation) -> None:
        """Authenticates the provided request information

        Args:
            request (RequestInformation): Request information object
        """
        return
