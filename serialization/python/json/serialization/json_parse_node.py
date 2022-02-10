from __future__ import annotations

from datetime import date, datetime, time, timedelta
from enum import Enum
from io import BytesIO
from typing import Any, Callable, Generic, List, Optional, Type, TypeVar
from uuid import UUID

from dateutil import parser
from kiota.abstractions.serialization import Parsable, ParseNode

T = TypeVar("T")

U = TypeVar("U", bound=Parsable)

K = TypeVar("K", bound=Enum)


class JsonParseNode(ParseNode, Generic[T, U, K]):

    on_before_assign_field_values: Optional[Callable[[Parsable], None]]
    on_after_assign_field_values: Optional[Callable[[Parsable], None]]

    def __init__(self, node: Any) -> None:
        """
        Args:
            node (Any):The JsonElement to initialize the node with
        """
        self._json_node = node

    def get_string_value(self) -> str:
        """Gets the string value from the json node
        Returns:
            str: The string value of the node
        """
        return str(self._json_node)

    def get_child_node(self, identifier: str) -> ParseNode:
        """Gets a new parse node for the given identifier
        Args:
            identifier (str): The identifier of the current node property
        Returns:
            Optional[ParseNode]: A new parse node for the given identifier
        """
        return JsonParseNode(self._json_node[identifier])

    def get_boolean_value(self) -> Optional[bool]:
        """Gets the boolean value of the json node
        Returns:
            bool: The boolean value of the node
        """
        return bool(self._json_node)

    def get_int_value(self) -> Optional[int]:
        """Gets the integer value of the json node
        Returns:
            int: The integer value of the node
        """
        return int(self._json_node)

    def get_float_value(self) -> Optional[float]:
        """Gets the float value of the json node
        Returns:
            float: The integer value of the node
        """
        return float(self._json_node)

    def get_uuid_value(self) -> Optional[UUID]:
        """Gets the UUID value of the json node
        Returns:
            UUID: The GUID value of the node
        """
        return UUID(self._json_node)

    def get_datetime_offset_value(self) -> Optional[datetime]:
        """Gets the datetime offset value of the json node
        Returns:
            datetime: The datetime offset value of the node
        """
        datetime_str = str(self._json_node)
        if datetime_str:
            datetime_obj = parser.parse(datetime_str)
            return datetime_obj
        return None

    def get_timedelta_value(self) -> Optional[timedelta]:
        """Gets the timedelta value of the node
        Returns:
            timedelta: The timedelta value of the node
        """
        datetime_str = str(self._json_node)
        if datetime_str:
            datetime_obj = parser.parse(datetime_str)
            return datetime.timedelta(hours=datetime_obj.hour,
                                      minutes=datetime_obj.minute,
                                      seconds=datetime_obj.second)
        return None

    def get_date_value(self) -> Optional[date]:
        """Gets the date value of the node
        Returns:
            date: The datevalue of the node in terms on year, month, and day.
        """
        datetime_str = str(self._json_node)
        if datetime_str:
            datetime_obj = parser.parse(datetime_str)
            return datetime_obj.date()
        return None

    def get_time_value(self) -> Optional[time]:
        """Gets the time value of the node
        Returns:
            time: The time value of the node in terms of hour, minute, and second.
        """
        datetime_str = str(self._json_node)
        if datetime_str:
            datetime_obj = parser.parse(datetime_str)
            return datetime_obj.time()
        return None

    def get_collection_of_primitive_values(self) -> Optional[List[T]]:
        """Gets the collection of primitive values of the node
        Returns:
            List[T]: The collection of primitive values
        """
        def func(item):
            current_parse_node = JsonParseNode(item)
            if isinstance(item, bool):
                return current_parse_node.get_boolean_value()
            elif isinstance(item, str):
                return current_parse_node.get_string_value()
            elif isinstance(item, int):
                return current_parse_node.get_int_value()
            elif isinstance(item, float):
                return current_parse_node.get_float_value()
            elif isinstance(item, UUID):
                return current_parse_node.get_uuid_value()
            elif isinstance(item, datetime):
                return current_parse_node.get_datetime_offset_value()
            elif isinstance(item, timedelta):
                return current_parse_node.get_timedelta_value()
            elif isinstance(item, date):
                return current_parse_node.get_time_value()
            elif isinstance(item, time):
                return current_parse_node.get_time_value()
            else:
                raise Exception(
                    f"Encountered an unknown type during deserialization {type(item)}"
                )

        return list(map(func, list(self._json_node)))

    def get_collection_of_object_values(self, class_type: Type[U]) -> List[U]:
        """Gets the collection of type U values from the json node
        Returns:
            List[U]: The collection of model object values of the node
        """
        def func(item):
            current_parse_node = JsonParseNode(item)
            return current_parse_node.get_object_value(class_type)

        return list(map(func, list(self._json_node)))

    def get_collection_of_enum_values(self, type: Any) -> List[K]:
        """Gets the collection of enum values of the json node
        Returns:
            List[K]: The collection of enum values
        """
        raw_values = str(self._json_node)
        if not raw_values:
            return []
        return list(map(lambda x: type(x.capitalize()), raw_values.split(",")))

    def get_enum_value(self, type: Any) -> Enum:
        """Gets the enum value of the node
        Returns:
            Enum: The enum value of the node
        """
        values = self.get_collection_of_enum_values(type)
        if values:
            return values[0]
        return None

    def get_object_value(self, class_type: Type[U]) -> U:
        """Gets the model object value of the node
        Returns:
            Parsable: The model object value of the node
        """
        result = class_type()
        if self.on_before_assign_field_values:
            self.on_before_assign_field_values(result)
        self.assign_field_values(result)
        if self.on_after_assign_field_values:
            self.on_after_assign_field_values(result)
        return result

    def assign_field_values(item: U) -> None:
        fields = item.get_field_deserializers()

    def get_byte_array_value(self) -> BytesIO:
        """Get a bytearray value from the nodes
        Returns:
            bytearray: The bytearray value from the nodes
        """
        return BytesIO(self._json_node)

    def get_on_before_assign_field_values(self) -> Callable[[Parsable], None]:
        """Gets the callback called before the node is deserialized.
        Returns:
            Callable[[Parsable], None]: the callback called before the node is deserialized.
        """
        return self.on_before_assign_field_values

    def get_on_after_assign_field_values(
            self) -> Optional[Callable[[Parsable], None]]:
        """Gets the callback called before the node is deserialized.
        Returns:
            Callable[[Parsable], None]: the callback called before the node is deserialized.
        """
        return self.on_after_assign_field_values

    def set_on_before_assign_field_values(
            self, value: Callable[[Parsable], None]) -> None:
        """Sets the callback called before the node is deserialized.
        Args:
            value (Callable[[Parsable], None]): the callback called before the node is
            deserialized.
        """
        self.on_before_assign_field_values = value

    def set_on_after_assign_field_values(
            self, value: Callable[[Parsable], None]) -> None:
        """Sets the callback called after the node is deserialized.
        Args:
            value (Callable[[Parsable], None]): the callback called after the node is
            deserialized.
        """
        self.on_after_assign_field_values = value
