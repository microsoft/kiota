from ..request_information import RequestInformation
from .access_token_provider import AccessTokenProvider
from .authentication_provider import AuthenticationProvider


class BaseBearerTokenAuthenticationProvider(AuthenticationProvider):
    """Provides a base class for implementing AuthenticationProvider for Bearer token scheme.
    """
    AUTHORIZATION_HEADER = "Authorization"

    def __init__(self, access_token_provider: AccessTokenProvider) -> None:
        self.access_token_provider = access_token_provider

    async def authenticate_request(self, request: RequestInformation) -> None:
        """Authenticates the provided RequestInformation instance using the provided
        authorization token

        Args:
            request (RequestInformation): Request information object
        """
        if not request:
            raise Exception("Request cannot be null")
        if not request.request_headers:
            request.headers = {}

        if not self.AUTHORIZATION_HEADER in request.headers:
            token = await self.access_token_provider.get_authorization_token(request.url)
            if token:
                request.add_request_headers({f'{self.AUTHORIZATION_HEADER}': f'Bearer {token}'})
