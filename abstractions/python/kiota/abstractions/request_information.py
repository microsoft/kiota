from io import BytesIO
from typing import TYPE_CHECKING, Any, Dict, Generic, List, Optional, Tuple, TypeVar

from uritemplate import URITemplate

from .method import Method
from .request_option import RequestOption
from .serialization import Parsable

if TYPE_CHECKING:
    from .request_adapter import RequestAdapter

Url = str
T = TypeVar("T", bound=Parsable)
QueryParams = TypeVar('QueryParams', int, float, str, bool, None)


class RequestInformation(Generic[QueryParams]):
    """This class represents an abstract HTTP request
    """
    RAW_URL_KEY = 'request-raw-url'
    BINARY_CONTENT_TYPE = 'application/octet-stream'
    CONTENT_TYPE_HEADER = 'Content-Type'

    # The uri of the request
    __uri: Optional[Url]

    __request_options: Dict[str, RequestOption] = {}

    # The path parameters for the current request
    path_parameters: Dict[str, Any] = {}

    # The URL template for the request
    url_template: Optional[str]

    # The HTTP Method for the request
    http_method: Method

    # The query parameters for the request
    query_parameters: Dict[str, QueryParams] = {}

    # The Request Headers
    headers: Dict[str, str] = {}

    # The Request Body
    content: BytesIO

    def get_url(self) -> Url:
        """ Gets the URL of the request
        """
        raw_url = self.path_parameters.get(self.RAW_URL_KEY)
        if self.__uri:
            return self.__uri
        if raw_url:
            return raw_url
        if not self.query_parameters:
            raise Exception("Query parameters cannot be null")
        if not self.path_parameters:
            raise Exception("Path parameters cannot be null")
        if not self.url_template:
            raise Exception("Url Template cannot be null")

        template = URITemplate(self.url_template)
        data: Dict[str, Any] = {}
        for key, val in self.query_parameters.items():
            data[key] = val
        for key, val in self.path_parameters.items():
            data[key] = val

        result = template.expand(data)
        return result

    def set_url(self, url: Url) -> None:
        """ Sets the URL of the request
        """
        if not url:
            raise Exception("Url cannot be undefined")
        self.__uri = url
        self.query_parameters.clear()
        self.path_parameters.clear()

    def get_request_options(self) -> List[Tuple[str, RequestOption]]:
        """Gets the request options for the request.
        """
        return list(self.__request_options.items())

    def add_request_options(self, options: List[RequestOption]) -> None:
        if not options:
            return
        for option in options:
            self.__request_options[option.get_key()] = option

    def remove_request_options(self, options: List[RequestOption]) -> None:
        if not options:
            return
        for option in options:
            del self.__request_options[option.get_key()]

    def set_content_from_parsable(
        self, request_adapter: Optional['RequestAdapter'], content_type: Optional[str],
        values: List[T]
    ) -> None:
        """Sets the request body from a model with the specified content type.

        Args:
            request_adapter (Optional[RequestAdapter]): The adapter service to get the serialization
            writer from.
            content_type (Optional[str]): the content type.
            values (List[T]): the models.
        """
        if not request_adapter:
            raise Exception("HttpCore cannot be undefined")
        if not content_type:
            raise Exception("HttpCore cannot be undefined")
        if not values:
            raise Exception("Values cannot be empty")

        writer = request_adapter.get_serialization_writer_factory(
        ).get_serialization_writer(content_type)
        self.headers[self.CONTENT_TYPE_HEADER] = content_type
        if len(values) == 1:
            writer.write_object_value(None, values[0])
        else:
            writer.write_collection_of_object_values(None, values)

        self.content = writer.get_serialized_content()

    def set_stream_content(self, value: BytesIO) -> None:
        """Sets the request body to be a binary stream.

        Args:
            value (BytesIO): the binary stream
        """
        self.headers[self.CONTENT_TYPE_HEADER] = self.BINARY_CONTENT_TYPE
        self.content = value
