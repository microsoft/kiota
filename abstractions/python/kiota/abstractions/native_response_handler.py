from typing import Any, Optional

from .response_handler import ModelType, NativeResponseType, ResponseHandler


class NativeResponseHandler(ResponseHandler):
    """Default response handler to access the native response object.
    """
    value: Optional[Any] = None

    async def handle_response_async(self, response: NativeResponseType) -> ModelType:
        self.value = response
        return self.value
