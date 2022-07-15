from abc import ABC, abstractmethod
from typing import Callable, Dict, Optional, TypeVar

from .serialization import Parsable, ParsableFactory

NativeResponseType = TypeVar("NativeResponseType")
ModelType = TypeVar("ModelType")


class ResponseHandler(ABC):
    """Abstract class that defines the contract for a response handler
    """

    @abstractmethod
    async def handle_response_async(
        self, response: NativeResponseType, error_map: Dict[str, Optional[ParsableFactory]]
    ) -> ModelType:
        """Callback method that is invoked when a response is received.

        Args:
            response (NativeResponseType): The type of the native response object.
            error_map (Dict[str, Optional[ParsableFactory]]): the error dict to use
            in case of a failed request.

        Returns:
            ModelType: The deserialized response.
        """
        pass
