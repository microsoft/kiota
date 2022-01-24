from abc import ABC, abstractmethod
from typing import TypeVar

from .serialization import Parsable

NativeResponseType = TypeVar("NativeResponseType")
ModelType = TypeVar("ModelType", bound=Parsable)


class ResponseHandler(ABC):
    """Abstract class that defines the contract for a response handler
    """
    @abstractmethod
    async def handle_response_async(self, response: NativeResponseType) -> ModelType:
        pass
