from typing import List, Set
from urllib.parse import urlparse


class AllowedHostsValidator:
    """Maintains a list of valid hosts and allows authentication providers to check whether
    a host is valid before authenticating a request
    """

    def __init__(self, allowed_hosts: List[str]) -> None:
        """Creates a new AllowedHostsValidator object with provided values.

        Args:
            allowed_hosts (List[str]): A list of valid hosts.  If the list is empty, all hosts
            are valid.
        """
        if not isinstance(allowed_hosts, list):
            raise TypeError("Allowed hosts must be a list of strings")

        self.allowed_hosts: Set[str] = {x.lower() for x in allowed_hosts}

    def get_allowed_hosts(self) -> List[str]:
        """Gets the list of valid hosts.  If the list is empty, all hosts are valid.

        Returns:
            List[str]: A list of valid hosts.  If the list is empty, all hosts are valid.
        """
        return list(self.allowed_hosts)

    def set_allowed_hosts(self, allowed_hosts: List[str]) -> None:
        """Sets the list of valid hosts.  If the list is empty, all hosts are valid.

        Args:
            allowed_hosts (List[str]): A list of valid hosts.  If the list is empty, all hosts
            are valid
        """
        if not isinstance(allowed_hosts, list):
            raise TypeError("Allowed hosts must be a list of strings")
        self.allowed_hosts = {x.lower() for x in allowed_hosts}

    def is_url_host_valid(self, url: str) -> bool:
        """Checks whether the provided host is valid.

        Args:
            url (str): The url to check.

        Returns:
            bool: [description]
        """
        if not url:
            return False
        if not self.allowed_hosts:
            return True

        # Format: urlparse("scheme://netloc/path;parameters?query#fragment")
        #Returns: ParseResult(scheme='scheme', netloc='netloc', path='/path;parameters', params='',
        #    query='query', fragment='fragment')
        o = urlparse(url)

        if o.netloc:
            return o.netloc.lower() in self.allowed_hosts

        return False
