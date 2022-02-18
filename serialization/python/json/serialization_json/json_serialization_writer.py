import base64
import json
from datetime import date, datetime, time, timedelta
from enum import Enum
from io import BytesIO
from typing import Any, Callable, Dict, List, Optional, TypeVar
from uuid import UUID

from kiota.abstractions.serialization import Parsable, SerializationWriter

T = TypeVar("T")
U = TypeVar("U", bound=Parsable)


class JsonSerializationWriter(SerializationWriter):

    PROPERTY_SEPARATOR: str = ','

    _on_start_object_serialization: Optional[Callable[[Parsable, SerializationWriter], None]] = None

    _on_before_object_serialization: Optional[Callable[[Parsable], None]] = None

    _on_after_object_serialization: Optional[Callable[[Parsable], None]] = None

    def __init__(self):
        self.writer: Dict = {}

    def write_string_value(self, key: Optional[str], value: Optional[str]) -> Optional[str]:
        """Writes the specified string value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[str]): The string value to be written.
        """
        if key and (value or value == ''):  #keeps empty strings as valid values
            self.writer[key] = value
        elif key and not (value or value == ''):
            self.write_null_value(key)
        elif (value or value == '') and not key:
            return value
        return None

    def write_boolean_value(self, key: Optional[str], value: Optional[bool]) -> Optional[bool]:
        """Writes the specified boolean value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[bool]): The boolean value to be written.
        """
        if key and (value or value is False):  # Escape False value from null check
            self.writer[key] = value
        elif key and not (value or value is False):
            self.write_null_value(key)
        elif (value or value is False) and not key:
            return value
        return None

    def write_int_value(self, key: Optional[str], value: Optional[int]) -> Optional[int]:
        """Writes the specified integer value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[int]): The integer value to be written.
        """
        if key and value:
            self.writer[key] = value
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            return value
        return None

    def write_float_value(self, key: Optional[str], value: Optional[float]) -> Optional[float]:
        """Writes the specified float value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[float]): The float value to be written.
        """
        if key and value:
            self.writer[key] = value
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            return value
        return None

    def write_uuid_value(self, key: Optional[str], value: Optional[UUID]) -> Optional[str]:
        """Writes the specified uuid value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[UUId]): The uuid value to be written.
        """
        if key and value:
            self.writer[key] = str(value)
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            return str(value)
        return None

    def write_datetime_offset_value(self, key: Optional[str],
                                    value: Optional[datetime]) -> Optional[str]:
        """Writes the specified datetime offset value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[datetime]): The datetime offset value to be written.
        """
        if key and value:
            self.writer[key] = str(value.isoformat())
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            return str(value.isoformat())
        return None

    def write_timedelta_value(self, key: Optional[str],
                              value: Optional[timedelta]) -> Optional[str]:
        """Writes the specified timedelta value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[timedelta]): The timedelta value to be written.
        """
        if key and value:
            self.writer[key] = str(value)
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            return str(value)
        return None

    def write_date_value(self, key: Optional[str], value: Optional[date]) -> Optional[str]:
        """Writes the specified date value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[date]): The date value to be written.
        """
        if key and value:
            self.writer[key] = str(value)
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            return str(value)
        return None

    def write_time_value(self, key: Optional[str], value: Optional[time]) -> Optional[str]:
        """Writes the specified time value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[time]): The time value to be written.
        """
        if key and value:
            self.writer[key] = str(value)
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            return str(value)
        return None

    def write_collection_of_primitive_values(self, key: Optional[str],
                                             values: Optional[List[T]]) -> Optional[List[T]]:
        """Writes the specified collection of primitive values to the stream with an optional
        given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            values (Optional[List[T]]): The collection of primitive values to be written.
        """
        if key and (values or values == []):  # Empty list is meaningful
            self.writer[key] = list(map(lambda x: self.write_any_value(None, x), values))
        elif key and not (values or values == []):
            self.write_null_value(key)
        elif (values or values == []) and not key:
            return list(map(lambda x: self.write_any_value(None, x), values))
        return None

    def write_collection_of_object_values(self, key: Optional[str],
                                          values: Optional[List[U]]) -> Optional[List[Dict]]:
        """Writes the specified collection of model objects to the stream with an optional
        given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            values (Optional[List[U]]): The collection of model objects to be written.
        """
        if key and (values or values == []):
            self.writer[key] = list(
                map(lambda x: self.write_object_value(None, x), values)
            )  # type: ignore
        elif key and not (values or values == []):
            self.write_null_value(key)
        elif (values or values == []) and not key:
            return list(map(lambda x: self.write_object_value(None, x), values))  # type: ignore
        return None

    def write_collection_of_enum_values(self, key: Optional[str],
                                        values: Optional[List[Enum]]) -> Optional[str]:
        """Writes the specified collection of enum values to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            values Optional[List[Enum]): The enum values to be written.
        """
        if key and (values or values == []):
            self.writer[key] = ','.join(
                list(map(lambda x: self.write_enum_value(None, x), values))  # type: ignore
            )
        elif key and not (values or values == []):
            self.write_null_value(key)
        elif (values or values == []) and key:
            return ','.join(
                list(map(lambda x: self.write_enum_value(None, x), values))  # type: ignore
            )
        return None

    def write_bytearray_value(self, key: Optional[str], value: bytes) -> Optional[str]:
        """Writes the specified byte array as a base64 string to the stream with an optional
        given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (bytes): The byte array to be written.
        """
        if key and value:
            base64_bytes = base64.b64encode(value)
            base64_string = base64_bytes.decode('utf-8')
            self.writer['key'] = base64_string
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            base64_bytes = base64.b64encode(value)
            base64_string = base64_bytes.decode('utf-8')
            return base64_string
        return None

    def write_object_value(self, key: Optional[str],
                           value: Optional[U]) -> Optional[Dict[Any, Any]]:
        """Writes the specified model object to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Parsable): The model object to be written.
        """
        if key and value:
            if self._on_before_object_serialization:
                self._on_before_object_serialization(value)
            if self._on_start_object_serialization:
                self._on_start_object_serialization(value, self)
            temp_writer = JsonSerializationWriter()
            value.serialize(temp_writer)
            if self._on_after_object_serialization:
                self._on_after_object_serialization(value)
            self.writer[key] = temp_writer.writer
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            if self._on_before_object_serialization:
                self._on_before_object_serialization(value)
            if self._on_start_object_serialization:
                self._on_start_object_serialization(value, self)
            temp_writer = JsonSerializationWriter()
            value.serialize(temp_writer)
            if self._on_after_object_serialization:
                self._on_after_object_serialization(value)
            return temp_writer.writer
        return None

    def write_enum_value(self, key: Optional[str], value: Optional[Enum]) -> Optional[str]:
        """Writes the specified enum value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (Optional[Enum]): The enum value to be written.
        """
        if key and value:
            self.writer[key] = value.name
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            return value.name
        return None

    def write_null_value(self, key: Optional[str]) -> None:
        """Writes a null value for the specified key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
        """
        if key:
            self.writer[key] = None

    def write_additional_data_value(self, value: Dict[str, Any]) -> None:
        """Writes the specified additional data to the stream.
        Args:
            value (Dict[str, Any]): he additional data to be written.
        """
        if value:
            for key, val in value.items():
                self.writer[key] = val

    def get_serialized_content(self) -> BytesIO:
        """Gets the value of the serialized content.
        Returns:
            BytesIO: The value of the serialized content.
        """
        json_string = json.dumps(self.writer)
        self.writer.clear()
        stream = BytesIO(json_string.encode('utf-8'))
        return stream

    def get_on_before_object_serialization(self) -> Optional[Callable[[Parsable], None]]:
        """Gets the callback called before the object gets serialized.
        Returns:
            Optional[Callable[[Parsable], None]]:the callback called before the object
            gets serialized.
        """
        return self._on_before_object_serialization

    def get_on_after_object_serialization(self) -> Optional[Callable[[Parsable], None]]:
        """Gets the callback called after the object gets serialized.
        Returns:
            Optional[Optional[Callable[[Parsable], None]]]: the callback called after the object
            gets serialized.
        """
        return self._on_after_object_serialization

    def get_on_start_object_serialization(
        self
    ) -> Optional[Callable[[Parsable, SerializationWriter], None]]:
        """Gets the callback called right after the serialization process starts.
        Returns:
            Optional[Callable[[Parsable, SerializationWriter], None]]: the callback called
            right after the serialization process starts.
        """
        return self._on_start_object_serialization

    def set_on_before_object_serialization(
        self, value: Optional[Callable[[Parsable], None]]
    ) -> None:
        """Sets the callback called before the objects gets serialized.
        Args:
            value (Optional[Callable[[Parsable], None]]): the callback called before the objects
            gets serialized.
        """
        self._on_before_object_serialization = value

    def set_on_after_object_serialization(
        self, value: Optional[Callable[[Parsable], None]]
    ) -> None:
        """Sets the callback called after the objects gets serialized.
        Args:
            value (Optional[Callable[[Parsable], None]]): the callback called after the objects
            gets serialized.
        """
        self._on_after_object_serialization = value

    def set_on_start_object_serialization(
        self, value: Optional[Callable[[Parsable, SerializationWriter], None]]
    ) -> None:
        """Sets the callback called right after the serialization process starts.
        Args:
            value (Optional[Callable[[Parsable, SerializationWriter], None]]): the callback
            called right after the serialization process starts.
        """
        self._on_start_object_serialization = value

    def write_non_parsable_object_value(self, key: Optional[str], value: T) -> Optional[dict]:
        """Writes the specified value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value (object): The value to be written.
        """
        if key and value:
            self.writer[key] = value.__dict__
        elif key and not value:
            self.write_null_value(key)
        elif value and not key:
            return value.__dict__
        return None

    def write_any_value(self, key: Optional[str], value: Any) -> Any:
        """Writes the specified value to the stream with an optional given key.
        Args:
            key (Optional[str]): The key to be used for the written value. May be null.
            value Any): The value to be written.
        """
        if key and value:
            value_type = type(value)
            if value_type == bool:
                self.write_boolean_value(key, value)
            elif value_type == str:
                self.write_string_value(key, value)
            elif value_type == int:
                self.write_int_value(key, value)
            elif value_type == float:
                self.write_float_value(key, value)
            elif value_type == UUID:
                self.write_uuid_value(key, value)
            elif value_type == datetime:
                self.write_datetime_offset_value(key, value)
            elif value_type == timedelta:
                self.write_timedelta_value(key, value)
            elif value_type == date:
                self.write_date_value(key, value)
            elif value_type == time:
                self.write_time_value(key, value)
            elif isinstance(value, Enum):
                self.write_enum_value(key, value)
            elif isinstance(value, Parsable):
                self.write_object_value(key, value)
            elif hasattr(value, '__dict__'):
                self.write_non_parsable_object_value(key, value)
            else:
                raise Exception(f"Encountered an unknown type during serialization {value_type}")

        elif key and not value:
            self.write_null_value(key)

        elif value and not key:
            value_type = type(value)
            if value_type == bool:
                return self.write_boolean_value(None, value)
            if value_type == str:
                return self.write_string_value(None, value)
            if value_type == int:
                return self.write_int_value(None, value)
            if value_type == float:
                return self.write_float_value(None, value)
            if value_type == UUID:
                return self.write_uuid_value(None, value)
            if value_type == datetime:
                return self.write_datetime_offset_value(None, value)
            if value_type == timedelta:
                return self.write_timedelta_value(None, value)
            if value_type == date:
                return self.write_date_value(None, value)
            if value_type == time:
                return self.write_time_value(None, value)
            if isinstance(value, Enum):
                return self.write_enum_value(None, value)
            if isinstance(value, Parsable):
                return self.write_object_value(None, value)
            if hasattr(value, '__dict__'):
                return self.write_non_parsable_object_value(None, value)
            raise Exception(f"Encountered an unknown type during serialization {value_type}")
        return None
