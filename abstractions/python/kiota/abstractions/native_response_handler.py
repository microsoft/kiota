from typing import Any, TypeVar

from .response_handler import ResponseHandler
from .serialization import Parsable

NativeResponseType = TypeVar("NativeResponseType")
ModelType = TypeVar("ModelType")


class NativeResponseHandler(ResponseHandler):
    """Default response handler to access the native response object.
    """
    # Native response object as returned by the core service
    value: Any

    async def handle_response_async(self, response: NativeResponseType) -> ModelType:
        self.value = response
        return None
