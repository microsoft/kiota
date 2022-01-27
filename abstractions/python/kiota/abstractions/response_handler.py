from abc import ABC, abstractmethod
from typing import TypeVar

NativeResponseType = TypeVar("NativeResponseType")
ModelType = TypeVar("ModelType")


class ResponseHandler(ABC):
    """Abstract class that defines the contract for a response handler
    """
    @abstractmethod
    async def handle_response_async(self, response: NativeResponseType) -> ModelType:
        """Callback method that is invoked when a response is received.

        Args:
            response (NativeResponseType): The type of the native response object.

        Returns:
            ModelType: The deserialized response.
        """
        pass
