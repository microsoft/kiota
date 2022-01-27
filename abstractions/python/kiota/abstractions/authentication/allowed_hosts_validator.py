from typing import List, Set


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

        scheme_and_rest = url.split("://")
        if len(scheme_and_rest) >= 2:
            rest = scheme_and_rest[1]
            if rest:
                return self._is_host_and_path_valid(rest)
            if url.startswith("http"):
                # protocol relative URL domain.tld/path
                return self._is_host_and_path_valid(url)
        return False

    def _is_host_and_path_valid(self, rest: str) -> bool:
        host_and_rest = rest.split("/")
        if len(host_and_rest) >= 2:
            host = host_and_rest[0]
            if host:
                return host.lower() in self.allowed_hosts
        return False
