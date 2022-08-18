from __future__ import annotations
from datetime import date, datetime
from typing import Any, Callable, Dict, List, Optional, TypeVar

from kiota.abstractions.serialization import AdditionalDataHolder, Parsable, ParseNode, SerializationWriter

from .office_location import OfficeLocation

T = TypeVar('T')


class User(Parsable, AdditionalDataHolder):

    def __init__(self) -> None:
        self._id: Optional[str] = None
        self._display_name: Optional[str] = None
        self._office_location: Optional[OfficeLocation] = None
        self._updated_at: Optional[datetime] = None
        self._birthday: Optional[date] = None
        self._business_phones: Optional[List[str]] = None
        self._mobile_phone: Optional[str] = None
        self._is_active: Optional[bool] = None
        self._age: Optional[int] = None
        self._gpa: Optional[float] = None
        self._additional_data: Optional[Dict[str, Any]] = {}

    @property
    def id(self):
        return self._id

    @id.setter
    def id(self, new_id):
        self._id = new_id

    @property
    def display_name(self):
        return self._display_name

    @display_name.setter
    def display_name(self, new_display_name):
        self._display_name = new_display_name

    @property
    def office_location(self):
        return self._office_location

    @office_location.setter
    def office_location(self, new_office_location):
        self._office_location = new_office_location

    @property
    def updated_at(self):
        return self._updated_at

    @updated_at.setter
    def updated_at(self, new_updated_at):
        self._updated_at = new_updated_at

    @property
    def birthday(self):
        return self._birthday

    @birthday.setter
    def birthday(self, new_birthday):
        self._birthday = new_birthday

    @property
    def business_phones(self):
        return self._business_phones

    @business_phones.setter
    def business_phones(self, new_business_phones):
        self._business_phones = new_business_phones

    @property
    def mobile_phone(self):
        return self._mobile_phone

    @mobile_phone.setter
    def mobile_phone(self, new_mobile_phone):
        self._mobile_phone = new_mobile_phone

    @property
    def is_active(self):
        return self._is_active

    @is_active.setter
    def is_active(self, new_is_active):
        self._is_active = new_is_active

    @property
    def age(self):
        return self._age

    @age.setter
    def age(self, new_age):
        self._age = new_age

    @property
    def gpa(self):
        return self._gpa

    @gpa.setter
    def gpa(self, new_gpa):
        self._gpa = new_gpa

    @property
    def additional_data(self) -> Dict[str, Any]:
        return self._additional_data

    @additional_data.setter
    def additional_data(self, data: Dict[str, Any]) -> None:
        self._additional_data = data

    @staticmethod
    def create_from_discriminator_value(parse_node: Optional[ParseNode] = None) -> User:
        """
        Creates a new instance of the appropriate class based on discriminator value
        Args:
            parseNode: The parse node to use to read the discriminator value and create the object
        Returns: Attachment
        """
        if not parse_node:
            raise Exception("parse_node cannot be undefined")
        return User()

    def get_field_deserializers(self) -> Dict[str, Callable[[ParseNode], None]]:
        """Gets the deserialization information for this object.

        Returns:
            Dict[str, Callable[[ParseNode], None]]: The deserialization information for this
            object where each entry is a property key with its deserialization callback.
        """
        return {
            "id":
            lambda n: setattr(self, 'id', n.get_uuid_value()),
            "display_name":
            lambda n: setattr(self, 'display_name', n.get_str_value()),
            "office_location":
            lambda n: setattr(self, 'office_location', n.get_enum_value(OfficeLocation)),
            "updated_at":
            lambda n: setattr(self, 'updated_at', n.get_datetime_value()),
            "birthday":
            lambda n: setattr(self, 'birthday', n.get_date_value()),
            "business_phones":
            lambda n: setattr(self, 'business_phones', n.get_collection_of_primitive_values(str)),
            "mobile_phone":
            lambda n: setattr(self, 'mobile_phone', n.get_str_value()),
            "is_active":
            lambda n: setattr(self, 'is_active', n.get_bool_value()),
            "age":
            lambda n: setattr(self, 'age', n.get_int_value()),
            "gpa":
            lambda n: setattr(self, 'gpa', n.get_float_value())
        }

    def serialize(self, writer: SerializationWriter) -> None:
        """Writes the objects properties to the current writer.

        Args:
            writer (SerializationWriter): The writer to write to.
        """
        if not writer:
            raise Exception("Writer cannot be undefined")
        writer.write_uuid_value("id", self.id)
        writer.write_str_value("display_name", self.display_name)
        writer.write_enum_value("office_location", self.office_location)
        writer.write_datetime_value("updated_at", self.updated_at)
        writer.write_date_value("birthday", self.birthday)
        writer.write_collection_of_primitive_values("business_phones", self.business_phones)
        writer.write_str_value("mobile_phone", self.mobile_phone)
        writer.write_bool_value("is_active", self.is_active)
        writer.write_int_value("age", self.age)
        writer.write_float_value("gpa", self.gpa)
