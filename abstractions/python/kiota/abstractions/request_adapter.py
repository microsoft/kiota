from abc import ABC, abstractmethod
from datetime import datetime
from io import BytesIO
from typing import List, Optional, TypeVar

from .request_information import RequestInformation
from .response_handler import ModelType, ResponseHandler
from .serialization import Parsable, SerializationWriterFactory
from .store import BackingStoreFactory

ResponseType = TypeVar("ResponseType", str, int, float, bool, datetime, BytesIO)


class RequestAdapter(ABC):
    """Service responsible for translating abstract Request Info into concrete native HTTP requests.
    """
    # The base url for every request.
    base_url = str

    @abstractmethod
    def get_serialization_writer_factory(self) -> SerializationWriterFactory:
        """Gets the serialization writer factory currently in use for the HTTP core service.

        Returns:
            SerializationWriterFactory: the serialization writer factory currently in use for the
            HTTP core service.
        """
        pass

    @abstractmethod
    async def send_async(
        self, request_info: RequestInformation, type: ModelType,
        response_handler: Optional[ResponseHandler]
    ) -> ModelType:
        """Excutes the HTTP request specified by the given RequestInformation and returns the
        deserialized response model.

        Args:
            request_info (RequestInformation): the request info to execute.
            type (ModelType): the class of the response model to deserialize the response into.
            response_handler (Optional[ResponseHandler]): The response handler to use for the HTTP
            request instead of the default handler.

        Returns:
            ModelType: the deserialized response model.
        """
        pass

    @abstractmethod
    async def send_collection_async(
        self, request_info: RequestInformation, type: ModelType,
        response_handler: Optional[ResponseHandler]
    ) -> List[ModelType]:
        """Excutes the HTTP request specified by the given RequestInformation and returns the
        deserialized response model collection.

        Args:
            request_info (RequestInformation): the request info to execute.
            type (ModelType): the class of the response model to deserialize the response into.
            response_handler (Optional[ResponseHandler]): The response handler to use for the
            HTTP request instead of the default handler.

        Returns:
            ModelType: the deserialized response model collection.
        """
        pass

    @abstractmethod
    async def send_collection_of_primitive_async(
        self, request_info: RequestInformation, response_type: ResponseType,
        response_handler: Optional[ResponseHandler]
    ) -> Optional[List[ResponseType]]:
        """Excutes the HTTP request specified by the given RequestInformation and returns the
        deserialized response model collection.

        Args:
            request_info (RequestInformation): the request info to execute.
            response_type (ResponseType): the class of the response model to deserialize the
            response into.
            response_handler (Optional[ResponseType]): The response handler to use for the HTTP
            request instead of the default handler.

        Returns:
            Optional[List[ModelType]]: he deserialized response model collection.
        """
        pass

    @abstractmethod
    async def send_primitive_async(
        self, request_info: RequestInformation, response_type: ResponseType,
        response_handler: Optional[ResponseHandler]
    ) -> ResponseType:
        """Excutes the HTTP request specified by the given RequestInformation and returns the
        deserialized primitive response model.

        Args:
            request_info (RequestInformation): the request info to execute.
            response_type (ResponseType): the class of the response model to deserialize the
            response into.
            response_handler (Optional[ResponseHandler]): The response handler to use for the
            HTTP request instead of the default handler.

        Returns:
            ResponseType: the deserialized primitive response model.
        """
        pass

    @abstractmethod
    async def send_no_response_content_async(
        self, request_info: RequestInformation, response_handler: Optional[ResponseHandler]
    ) -> None:
        """Excutes the HTTP request specified by the given RequestInformation and returns the
        deserialized primitive response model.

        Args:
            request_info (RequestInformation):the request info to execute.
            response_handler (Optional[ResponseHandler]): The response handler to use for the
            HTTP request instead of the default handler.
        """
        pass

    @abstractmethod
    def enable_backing_store(self, backing_store_factory: Optional[BackingStoreFactory]) -> None:
        """Enables the backing store proxies for the SerializationWriters and ParseNodes in use.

        Args:
            backing_store_factory (Optional[BackingStoreFactory]): the backing store factory to use.
        """
        pass
