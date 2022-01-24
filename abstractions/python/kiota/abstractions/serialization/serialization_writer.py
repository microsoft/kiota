from __future__ import annotations

from abc import ABC
from datetime import datetime
from enum import Enum
from io import BytesIO
from typing import Any, Callable, Dict, List, Optional, TypeVar
from uuid import UUID

from .parsable import Parsable

T = TypeVar("T")
U = TypeVar("U", bound=Parsable)


class SerializationWriter(ABC):
    """Defines an interface for serialization of objects to a stream
    """
    def write_string_value(self, key: Optional[str], value: Optional[str]) -> None:
        """Writes the specified string value to the stream with an optional given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[str]): The string value to be written.
        """
        pass

    def write_boolean_value(self, key: Optional[str], value: Optional[bool]) -> None:
        """Writes the specified boolean value to the stream with an optional given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[bool]): The boolean value to be written.
        """
        pass

    def write_int_value(self, key: Optional[str], value: Optional[int]) -> None:
        """Writes the specified integer value to the stream with an optional given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[int]): The integer value to be written.
        """
        pass

    def write_float_value(self, key: Optional[str], value: Optional[float]) -> None:
        """Writes the specified float value to the stream with an optional given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[float]): The float value to be written.
        """
        pass

    def write_uuid_value(self, key: Optional[str], value: Optional[UUID]) -> None:
        """Writes the specified uuid value to the stream with an optional given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[UUId]): The uuid value to be written.
        """
        pass

    def write_datetime_offset_value(self, key: Optional[str], value: Optional[datetime]) -> None:
        """Writes the specified datetime offset value to the stream with an optional given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[datetime]): The datetime offset value to be written.
        """
        pass

    def write_collection_of_primitive_values(
        self, key: Optional[str], values: Optional[List[T]]
    ) -> None:
        """Writes the specified collection of primitive values to the stream with an optional
        given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            values (Optional[List[T]]): The collection of primitive values to be written.
        """
        pass

    def write_collection_of_object_values(
        self, key: Optional[str], values: Optional[List[U]]
    ) -> None:
        """Writes the specified collection of model objects to the stream with an optional
        given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            values (Optional[List[U]]): The collection of model objects to be written.
        """
        pass

    def write_collection_of_enum_values(
        self, key: Optional[str], values: Optional[List[Enum]]
    ) -> None:
        """Writes the specified collection of enum values to the stream with an optional given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            values Optional[List[Enum]): The enum values to be written.
        """
        pass

    def write_bytearray_value(self, key: Optional[str], value: BytesIO) -> None:
        """Writes the specified byte array as a base64 string to the stream with an optional
        given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (BytesIO): The byte array to be written.
        """
        pass

    def write_object_value(self, key: Optional[str], value: U) -> None:
        """Writes the specified model object to the stream with an optional given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Parsable): The model object to be written.
        """
        pass

    def write_enum_value(self, key: Optional[str], value: Optional[Enum]) -> None:
        """Writes the specified enum value to the stream with an optional given key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[Enum]): The enum value to be written.
        """
        pass

    def write_null_value(self, key: Optional[str]) -> None:
        """Writes a null value for the specified key.

        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
        """
        pass

    def write_additional_data_value(self, value: Dict[str, Any]) -> None:
        """Writes the specified additional data to the stream.
        Args:
            value (Dict[str, Any]): he additional data to be written.
        """
        pass

    def get_serialized_content(self) -> BytesIO:
        """Gets the value of the serialized content.

        Returns:
            BytesIO: The value of the serialized content.
        """
        pass

    def on_before_object_serialization(self) -> Optional[Callable[[Parsable], None]]:
        """Gets the callback called before the object gets serialized.

        Returns:
            Optional[Callable[[Parsable], None]]:the callback called before the object
            gets serialized.
        """
        pass

    def on_after_object_serialization(self) -> Optional[Callable[[Parsable], None]]:
        """Gets the callback called after the object gets serialized.

        Returns:
            Optional[Optional[Callable[[Parsable], None]]]: the callback called after the object
            gets serialized.
        """
        pass

    def on_start_object_serialization(
        self
    ) -> Optional[Callable[[Parsable, SerializationWriter], None]]:
        """Gets the callback called right after the serialization process starts.

        Returns:
            Optional[Callable[[Parsable, SerializationWriter], None]]: the callback called
            right after the serialization process starts.
        """
        pass
