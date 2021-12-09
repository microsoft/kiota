from abc import ABC, abstractmethod
from typing import Coroutine

from ..request_information import RequestInformation

class AuthenticationProvider(ABC):
    """
    Authenticates the application request
    """
    
    @abstractmethod
    def authenticate_request(self, request: RequestInformation) -> Coroutine:
        """Authenticates the application request

        Args:
            request (RequestInformation): The request to authenticate
        """
        pass