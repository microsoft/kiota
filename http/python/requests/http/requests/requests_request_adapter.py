import requests
from datetime import date, datetime
from typing import Dict, List, Optional, TypeVar

from urllib3 import Retry



from kiota.abstractions.authentication import AuthenticationProvider
from kiota.abstractions.serialization import Parsable, ParseNode, ParseNodeFactory, ParseNodeFactoryRegistry, SerializationWriterFactoryRegistry, SerializationWriterFactory
from kiota.abstractions.store import BackingStoreFactory, BackingStoreFactorySingleton
from kiota.abstractions.api_client_builder import enable_backing_store_for_serialization_writer_factory, enable_backing_store_for_parse_node_factory
from kiota.abstractions.request_adapter import Responsetype, RequestAdapter
from kiota.abstractions.request_information import RequestInformation
from kiota.abstractions.response_handler import ResponseHandler

from .http_client import HttpClient

ResponseType = TypeVar("ResponseType", str, int, float, bool, datetime, bytes)
ModelType = TypeVar("ModelType", bound=Parsable)

class RequestsRequestAdapter(RequestAdapter):
    # The base urlf for every request
    base_url: str = ''
    
    def __init__(self, authentication_provider: AuthenticationProvider,
                 parse_node_factory: ParseNodeFactory = ParseNodeFactoryRegistry(),
                 serialization_writer_factory: SerializationWriterFactory = SerializationWriterFactoryRegistry(),
                 http_client: HttpClient = HttpClient()) -> None:
    
        if not authentication_provider:
            raise TypeError("Authentication provider cannot be null")
        if not parse_node_factory:
            raise TypeError("Parse node factory cannot be null")
        if not serialization_writer_factory:
            raise TypeError("Serialization writer factory cannot be null")
        if not http_client:
            raise TypeError("Http Client cannot be null")
        
        self._authentication_provider = authentication_provider
        self._parse_node_factory = parse_node_factory
        self._serialization_writer_factory = serialization_writer_factory
        self._http_client = http_client
    
    def get_serialization_writer_factory(self) -> SerializationWriterFactory:
        """Gets the serialization writer factory currently in use for the HTTP core service.
        Returns:
            SerializationWriterFactory: the serialization writer factory currently in use for the
            HTTP core service.
        """
        return self._serialization_writer_factory
    
    def get_response_content_type(response: requests.Response) -> Optional[str]:
        content_type = response.headers.get("content-type")
        if content_type:
            return content_type.lower()
        return None

    async def send_async(
        self, request_info: RequestInformation, model_type: ModelType,
        response_handler: Optional[ResponseHandler],
        error_dict: Dict[str, Parsable] = {}
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
        if not request_info:
            raise TypeError("Request info cannot be null")
        
        response = await self.get_http_response_message(request_info)
        
        if response_handler:
            return await response_handler.handle_response_async(response, error_dict)
        else:
            await self.throw_failed_responses(response, error_dict)
            root_node = await self.get_root_parse_node(response)
            result = root_node.get_object_value(model_type)
            return result

    async def send_collection_async(
        self, request_info: RequestInformation, model_type: ModelType,
        response_handler: Optional[ResponseHandler],
        error_dict: Dict[str, Parsable] = {}
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
        if not request_info:
            raise TypeError("Request info cannot be null")
        
        response = await self.get_http_response_message(request_info)
        
        if response_handler:
            return await response_handler.handle_response_async(response, error_dict)
        else:
            await self.throw_failed_responses(response, error_dict)
            root_node = await self.get_root_parse_node(response)
            result = root_node.get_collection_of_object_values(model_type)
            return result

    async def send_collection_of_primitive_async(
        self, request_info: RequestInformation, response_type: ResponseType,
        response_handler: Optional[ResponseHandler],
        error_dict: Dict[str, Parsable] = {}
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
        if not request_info:
            raise TypeError("Request info cannot be null")
        
        response = await self.get_http_response_message(request_info)
        
        if response_handler:
            return await response_handler.handle_response_async(response, error_dict)
        else:
            await self.throw_failed_responses(response, error_dict)
            root_node = await self.get_root_parse_node(response)
            if isinstance(response_type, str):
                return root_node.get_collection_of_primitive_values[str]()
            elif isinstance(response_type, int):
                return root_node.get_collection_of_primitive_values[int]()
            elif isinstance(response_type, float):
                return root_node.get_collection_of_primitive_values[float]()
            elif isinstance(response_type, bool):
                return root_node.get_collection_of_primitive_values[bool]()
            elif isinstance(response_type, datetime):
                return root_node.get_collection_of_primitive_values[datetime]()
            raise Exception("Found unexpected type to deserialize")
            
            

    async def send_primitive_async(
        self, request_info: RequestInformation, response_type: ResponseType,
        response_handler: Optional[ResponseHandler],
        error_dict: Dict[str, Parsable] = {}
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
        if not request_info:
            raise TypeError("Request info cannot be null")
        
        response = await self.get_http_response_message(request_info)
        
        if response_handler:
            return await response_handler.handle_response_async(response, error_dict)
        else:
            await self.throw_failed_responses(response, error_dict)
            root_node = await self.get_root_parse_node(response)
            if isinstance(response_type, str):
                return root_node.get_string_value()
            elif isinstance(response_type, int):
                return root_node.get_int_value()
            elif isinstance(response_type, float):
                return root_node.get_float_value()
            elif isinstance(response_type, bool):
                return root_node.get_boolean_value()
            elif isinstance(response_type, datetime):
                return root_node.get_datetime_value()
            elif isinstance(response_type, bytes):
                return root_node.get_bytearray_value()
            raise Exception("Found unexpected type to deserialize")

    async def send_no_response_content_async(
        self, request_info: RequestInformation, response_handler: Optional[ResponseHandler],
        error_dict: Dict[str, Parsable] = {}
    ) -> None:
        """Excutes the HTTP request specified by the given RequestInformation and returns the
        deserialized primitive response model.
        Args:
            request_info (RequestInformation):the request info to execute.
            response_handler (Optional[ResponseHandler]): The response handler to use for the
            HTTP request instead of the default handler.
        """
        if not request_info:
            raise TypeError("Request info cannot be null")
        
        response = await self.get_http_response_message(request_info)
        
        if response_handler:
            return await response_handler.handle_response_async(response, error_dict)
        await self.throw_failed_responses(response, error_dict)

    def enable_backing_store(self, backing_store_factory: Optional[BackingStoreFactory]) -> None:
        """Enables the backing store proxies for the SerializationWriters and ParseNodes in use.
        Args:
            backing_store_factory (Optional[BackingStoreFactory]): the backing store factory to use.
        """
        self._parse_node_factory = enable_backing_store_for_parse_node_factory(self._parse_node_factory)
        self._serialization_writer_factory = enable_backing_store_for_serialization_writer_factory(self._serialization_writer_factory)
        if not (self._serialization_writer_factory or self._parse_node_factory):
            raise Exception("Unable to enable backing store")
        if backing_store_factory:
            BackingStoreFactorySingleton.__instance = backing_store_factory
            
    async def get_root_parse_node(self, response: requests.Response) -> ParseNode:
        payload = await response
        response_content_type = self.get_response_content_type(response)
        if not response_content_type:
            raise Exception("No response content type found for deserialization")
        
        return self._parse_node_factory.get_root_parse_node(response_content_type, payload)
    
    async def throw_failed_responses(self, response: requests.Response, error_dict: Dict[str, Parsable] = {}) -> None:
        if response.ok:
            return
        
        status_code = str(response.status_code)
        
    async def get_http_response_message(self, request_info: RequestInformation) -> requests.Response:
        if not request_info:
            raise Exception("Request info cannot be null")
        
        self.set_base_url_for_request_information(request_info)
        await self._authentication_provider.authenticate_request(request_info)
        
        request = self.get_request_from_request_information(request_info)
        return await self._http_client.fetch(request_info.get_url(), request)
    
    def set_base_url_for_request_information(self, request_info: RequestInformation) -> None:
        request_info.path_parameters["base_url"] = self.base_url
        
    def get_request_from_request_information(request_info: RequestInformation) -> Dict:
        
        request = {
            "method": str(request_info.http_method),
            "headers": request_info.headers,
            "body": request_info.content
        }
        
        return request
    
            
            
        
        

