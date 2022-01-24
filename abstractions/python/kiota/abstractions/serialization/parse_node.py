from __future__ import annotations

from abc import ABC, abstractmethod
from datetime import datetime
from enum import Enum
from io import BytesIO
from typing import Callable, List, Optional, TypeVar
from uuid import UUID

from .parsable import Parsable

T = TypeVar("T")

U = TypeVar("U", bound=Parsable)

K = TypeVar("K", bound=Enum)


class ParseNode(ABC):
    """
    Interface for a deserialization node in a parse tree. This interace provides an abstraction
    layer over serialization formats, libraries and implementations.
    """
    @abstractmethod
    def get_string_value(self) -> str:
        """Gets the string value of the node

        Returns:
            str: The string value of the node
        """
        pass

    @abstractmethod
    def get_child_node(self, identifier: str) -> ParseNode:
        """Gets a new parse node for the given identifier

        Args:
            identifier (str): The identifier of the current node property

        Returns:
            ParseNode: A new parse node for the given identifier
        """
        pass

    @abstractmethod
    def get_boolean_value(self) -> Optional[bool]:
        """Gets the boolean value of the node

        Returns:
            bool: The boolean value of the node
        """
        pass

    @abstractmethod
    def get_int_value(self) -> Optional[int]:
        """Gets the integer value of the node

        Returns:
            int: The integer value of the node
        """
        pass

    @abstractmethod
    def get_float_value(self) -> Optional[float]:
        """Gets the float value of the node

        Returns:
            float: The integer value of the node
        """
        pass

    @abstractmethod
    def get_uuid_value(self) -> Optional[UUID]:
        """Gets the UUID value of the node

        Returns:
            UUID: The GUID value of the node
        """
        pass

    @abstractmethod
    def get_datetime_offset_value(self) -> Optional[datetime]:
        """Gets the datetime offset value of the node

        Returns:
            datetime: The datetime offset value of the node
        """
        pass

    @abstractmethod
    def get_collection_of_primitive_values(self) -> List[T]:
        """Gets the collection of primitive values of the node

        Returns:
            List[T]: The collection of primitive values
        """
        pass

    @abstractmethod
    def get_collection_of_object_values(self) -> List[U]:
        """Gets the collection of model object values of the node

        Returns:
            List[U]: The collection of model object values of the node
        """
        pass

    @abstractmethod
    def get_collection_of_enum_values(self) -> List[K]:
        """Gets the collection of enum values of the node

        Returns:
            List[K]: The collection of enum values
        """
        pass

    @abstractmethod
    def get_enum_value(self) -> Enum:
        """Gets the enum value of the node

        Returns:
            Enum: The enum value of the node
        """
        pass

    @abstractmethod
    def get_object_value(self) -> U:
        """Gets the model object value of the node

        Returns:
            Parsable: The model object value of the node
        """
        pass

    @abstractmethod
    def get_byte_array_value(self) -> BytesIO:
        """Get a bytearray value from the nodes

        Returns:
            bytearray: The bytearray value from the nodes
        """
        pass

    @abstractmethod
    def on_before_assign_field_values(self) -> Callable[[Parsable], None]:
        """Gets the callback called before the node is deserialized.

        Returns:
            Callable[[Parsable], None]: the callback called before the node is deserialized.
        """
        pass

    @abstractmethod
    def on_after_assign_field_values(self) -> Optional[Callable[[Parsable], None]]:
        """Gets the callback called before the node is deserialized.

        Returns:
            Callable[[Parsable], None]: the callback called before the node is deserialized.
        """
        pass
