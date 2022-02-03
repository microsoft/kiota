from typing import Dict, List, Optional, Set

from kiota.abstractions.authentication import AccessTokenProvider, AllowedHostsValidator

from azure.core.credentials_async import AsyncTokenCredential


class AzureIdentityAccessTokenProvider(AccessTokenProvider):
    """Access token provider that leverages the Azure Identity library to retrieve an access token.
    """

    def __init__(
        self,
        credentials: AsyncTokenCredential,
        options: Optional[Dict],
        scopes: List[str] = ['https://graph.microsoft.com/.default'],
        allowed_hosts: Set[str] = {
            'graph.microsoft.com', 'graph.microsoft.us', 'dod-graph.microsoft.us',
            'graph.microsoft.de', 'microsoftgraph.chinacloudapi.cn', 'canary.graph.microsoft.com'
        },
    ) -> None:
        if not credentials:
            raise Exception("Parameter credentials cannot be null")
        if not scopes:
            raise Exception("Scopes cannot be null")

        self._credentials = credentials
        self._scopes = scopes
        self._options = options
        self._allowed_hosts_validator = AllowedHostsValidator(allowed_hosts)

    async def get_authorization_token(self, uri: str) -> str:
        """This method is called by the BaseBearerTokenAuthenticationProvider class to get the
        access token.
        Args:
            uri (str): The target URI to get an access token for.
        Returns:
            str: The access token to use for the request.
        """
        if not uri or not self.get_allowed_hosts_validator().is_url_host_valid(uri):
            return ""

        if self._options:
            result = await self._credentials.get_token(*self._scopes, **self._options)
        result = await self._credentials.get_token(*self._scopes)
        if result and result.token:
            return result.token
        return ""

    def get_allowed_hosts_validator(self) -> AllowedHostsValidator:
        """Retrieves the allowed hosts validator.
        Returns:
            AllowedHostsValidator: The allowed hosts validator.
        """
        return self._allowed_hosts_validator
