from abc import ABC, abstractmethod

from .allowed_hosts_validator import AllowedHostsValidator


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

    @abstractmethod
    def get_allowed_hosts_validator(self) -> AllowedHostsValidator:
        """Retrieves the allowed hosts validator.

        Returns:
            AllowedHostsValidator: The allowed hosts validator.
        """
        pass
