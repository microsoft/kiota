from abc import ABC, abstractmethod


class RequestOption(ABC):
    """Represents a request option
    """

    @abstractmethod
    def get_key(self) -> str:
        """Gets the option key for when adding it to a request. Must be unique

        Returns:
            str: The option key
        """
        pass
