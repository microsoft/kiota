from abc import ABC, abstractmethod
from io import BytesIO

from .parse_node import ParseNode


class ParseNodeFactory(ABC):
    """Defines the protocol for a factory that is used to create ParseNodes.
    """

    @abstractmethod
    def get_valid_content_type(self) -> str:
        """Returns the content type this factory's parse nodes can deserialize

        Returns:
            str: The content type to be deserialized
        """
        pass

    @abstractmethod
    def get_root_parse_node(self, content_type: str, content: BytesIO) -> ParseNode:
        """Creates a ParseNode from the given binary stream and content type

        Args:
            content_type (str): The content type of the binary stream
            content (BytesIO): The array buffer to read from

        Returns:
            ParseNode: A ParseNode that can deserialize the given binary stream
        """
        pass
