from abc import ABC, abstractmethod


class AccessTokenProvider(ABC):
    """Defines a contract for obtaining access tokens for a given url.
    """
    @abstractmethod
    async def get_authorization_token(self, uri: str) -> str:
        """This method is called by the BaseBearerTokenAuthenticationProvider class to get the
        access token.

        Args:
            uri (str): The target URI to get an access token for.
        Returns:
            str: The access token to use for the request.
        """
        pass
