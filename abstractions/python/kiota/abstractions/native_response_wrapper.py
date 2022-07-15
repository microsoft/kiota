from token import OP
from typing import Awaitable, Callable, Optional, TypeVar

from .native_response_handler import NativeResponseHandler
from .request_option import RequestOption
from .response_handler import ResponseHandler

ModelType = TypeVar("ModelType")
QueryParametersType = TypeVar("QueryParametersType")
HeadersType = TypeVar("HeadersType")
RequestBodyType = TypeVar("RequestBodyType")
NativeResponseType = TypeVar("NativeResponseType")

OriginalCallType = Callable[[
    Optional[QueryParametersType], Optional[HeadersType], Optional[RequestOption],
    Optional[ResponseHandler]
], Awaitable[ModelType]]
OriginalCallWithBodyType = Callable[[
    RequestBodyType, Optional[QueryParametersType], Optional[HeadersType], Optional[RequestOption],
    Optional[ResponseHandler]
], Awaitable[ModelType]]


class NativeResponseWrapper:
    """This class can be used to wrap a request using the fluent API and get the native response
    object in return.
    """

    async def call_and_get_native(
        self, original_call: OriginalCallType, q: Optional[QueryParametersType],
        h: Optional[HeadersType], o: Optional[RequestOption]
    ) -> NativeResponseType:
        response_handler = NativeResponseHandler()
        await original_call(q, h, o, response_handler)
        return response_handler.value

    async def call_and_get_native_with_body(
        self, original_call: OriginalCallWithBodyType, request_body: RequestBodyType,
        q: Optional[QueryParametersType], h: Optional[HeadersType], o: Optional[RequestOption]
    ) -> NativeResponseType:
        response_handler = NativeResponseHandler()
        await original_call(request_body, q, h, o, response_handler)
        return response_handler.value
