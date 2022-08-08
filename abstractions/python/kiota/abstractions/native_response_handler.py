from typing import Any, Callable, Dict, Optional, TypeVar, cast

from .response_handler import ResponseHandler
from .serialization import Parsable, ParsableFactory

NativeResponseType = TypeVar("NativeResponseType")
ModelType = TypeVar("ModelType")


class NativeResponseHandler(ResponseHandler):
    """Default response handler to access the native response object.
    """
    # Native response object as returned by the core service
    value: Any

    # The error mappings for the response to use when deserializing failed responses bodies.
    # Where an error code like 401 applies specifically to that status code, a class code like
    # 4XX applies to all status codes within the range if a specific error code is not present.
    error_map: Dict[str, Optional[ParsableFactory]]

    async def handle_response_async(
        self, response: NativeResponseType, error_map: Dict[str, Optional[ParsableFactory]]
    ) -> ModelType:
        self.value = response
        self.error_map = error_map
        return cast(ModelType, None)
