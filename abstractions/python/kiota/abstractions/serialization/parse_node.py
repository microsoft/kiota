from __future__ import annotations

from abc import ABC, abstractmethod
from datetime import date, datetime, time, timedelta
from enum import Enum
from io import BytesIO
from typing import TYPE_CHECKING, Callable, List, Optional, TypeVar
from uuid import UUID

from .parsable import Parsable

T = TypeVar("T")

U = TypeVar("U", bound=Parsable)

K = TypeVar("K", bound=Enum)

if TYPE_CHECKING:
    from .parsable_factory import ParsableFactory


class ParseNode(ABC):
    """
    Interface for a deserialization node in a parse tree. This interace provides an abstraction
    layer over serialization formats, libraries and implementations.
    """

    @abstractmethod
    def get_str_value(self) -> str:
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
    def get_bool_value(self) -> Optional[bool]:
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
    def get_datetime_value(self) -> Optional[datetime]:
        """Gets the datetime value of the node

        Returns:
            datetime: The datetime value of the node
        """
        pass

    @abstractmethod
    def get_timedelta_value(self) -> Optional[timedelta]:
        """Gets the timedelta value of the node

        Returns:
            timedelta: The timedelta value of the node
        """
        pass

    @abstractmethod
    def get_date_value(self) -> Optional[date]:
        """Gets the date value of the node

        Returns:
            date: The datevalue of the node in terms on year, month, and day.
        """
        pass

    @abstractmethod
    def get_time_value(self) -> Optional[time]:
        """Gets the time value of the node

        Returns:
            time: The time value of the node in terms of hour, minute, and second.
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
    def get_collection_of_object_values(self, factory: ParsableFactory) -> List[U]:
        """Gets the collection of model object values of the node
        Args:
            factory (ParsableFactory): The factory to use to create the model object.
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
    def get_object_value(self, factory: ParsableFactory) -> U:
        """Gets the model object value of the node
        Args:
            factory (ParsableFactory): The factory to use to create the model object.
        Returns:
            Parsable: The model object value of the node
        """
        pass

    @abstractmethod
    def get_bytes_value(self) -> BytesIO:
        """Get a bytearray value from the nodes

        Returns:
            bytearray: The bytearray value from the nodes
        """
        pass

    @abstractmethod
    def get_on_before_assign_field_values(self) -> Callable[[Parsable], None]:
        """Gets the callback called before the node is deserialized.

        Returns:
            Callable[[Parsable], None]: the callback called before the node is deserialized.
        """
        pass

    @abstractmethod
    def get_on_after_assign_field_values(self) -> Optional[Callable[[Parsable], None]]:
        """Gets the callback called before the node is deserialized.

        Returns:
            Callable[[Parsable], None]: the callback called before the node is deserialized.
        """
        pass

    @abstractmethod
    def set_on_before_assign_field_values(self, value: Callable[[Parsable], None]) -> None:
        """Sets the callback called before the node is deserialized.

        Args:
            value (Callable[[Parsable], None]): the callback called before the node is
            deserialized.
        """
        pass

    @abstractmethod
    def set_on_after_assign_field_values(self, value: Callable[[Parsable], None]) -> None:
        """Sets the callback called after the node is deserialized.

        Args:
            value (Callable[[Parsable], None]): the callback called after the node is
            deserialized.
        """
        pass
