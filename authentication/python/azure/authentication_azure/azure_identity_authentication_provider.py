from typing import Dict, List, Optional, Set

from kiota.abstractions import BaseBearerTokenAuthenticationProvider

from azure.core.credentials_async import AsyncTokenCredential

from .azure_identity_access_token_provider import AzureIdentityAccessTokenProvider


class AzureIdentityAuthenticationProvider(BaseBearerTokenAuthenticationProvider
                                          ):
    def __init__(
        self,
        credentials: AsyncTokenCredential,
        options: Optional[Dict],
        scopes: List[str] = ['https://graph.microsoft.com/.default'],
        allowed_hosts: Set[str] = {
            'graph.microsoft.com', 'graph.microsoft.us',
            'dod-graph.microsoft.us', 'graph.microsoft.de',
            'microsoftgraph.chinacloudapi.cn', 'canary.graph.microsoft.com'
        }
    ) -> None:
        """[summary]

        Args:
            credentials (AsyncTokenCredential): The tokenCredential implementation to use for
            authentication.
            options (Optional[dict]): The options to use for authentication.
            scopes (List[str], optional): he scopes to use for authentication. Defaults to
            ['https://graph.microsoft.com/.default'].
            allowed_hosts (Set[str], optional): The allowed hosts to use for authentication.
            Defaults to {'graph.microsoft.com', 'graph.microsoft.us', 'dod-graph.microsoft.us',
            'graph.microsoft.de', 'microsoftgraph.chinacloudapi.cn', 'canary.graph.microsoft.com'}.
        """
        super().init(
            AzureIdentityAccessTokenProvider(credentials, options, scopes,
                                             allowed_hosts))
