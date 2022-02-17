from datetime import date, datetime
from typing import Any, Callable, Dict, List, Optional, TypeVar

from kiota.abstractions.serialization import Parsable, ParseNode, SerializationWriter

from .office_location import OfficeLocation

T = TypeVar('T')

class User(Parsable):
    _id: Optional[str] = None
    _display_name: Optional[str] = None
    _office_location: Optional[OfficeLocation] = None
    _updated_at: Optional[datetime] = None
    _birthday: Optional[date] = None
    _business_phones: Optional[List[str]] = None
    _mobile_phone: Optional[str] = None
    _is_active: Optional[bool] = None
    _age: Optional[int] = None
    _gpa: Optional[float] = None
    _additional_data: Optional[Dict[str, Any]] = None
    

    def get_id(self):
        return self._id

    def set_id(self, new_id):
        self._id = new_id
        
    def get_display_name(self):
        return self._display_name

    def set_display_name(self, new_display_name):
        self._display_name = new_display_name
        
    def get_office_location(self):
        return self._office_location

    def set_office_location(self, new_office_location):
        self._office_location = new_office_location
        
    def get_updated_at(self):
        return self._updated_at
       
    def set_updated_at(self, new_updated_at):
        self._updated_at = new_updated_at
        
    def get_birthday(self):
        return self._birthday 
    
    def set_birthday(self, new_birthday):
        self._birthday = new_birthday
        
    def get_business_phones(self):
        return self._business_phones

    def set_business_phones(self, new_business_phones):
        self._business_phones = new_business_phones
        
    def get_mobile_phone(self):
        return self._mobile_phone
    
    def set_mobile_phone(self, new_mobile_phone):
        self._mobile_phone = new_mobile_phone
        
    def get_is_active(self):
        return self._is_active
    
    def set_is_active(self, new_is_active):
        self._is_active = new_is_active
        
    def get_age(self):
        return self._age
    
    def set_age(self, new_age):
        self._age = new_age
        
    def get_gpa(self):
        return self._gpa
    
    def set_gpa(self, new_gpa):
        self._gpa = new_gpa
        
    def get_additional_data(self) -> Dict[str, Any]:
        return self._additional_data
    
    def set_additional_data(self, data: Dict[str, Any]) -> None:
        self._additional_data = data

    def get_field_deserializers(self) -> Dict[str, Callable[[T, ParseNode], None]]:
        """Gets the deserialization information for this object.

        Returns:
            Dict[str, Callable[[T, ParseNode], None]]: The deserialization information for this
            object where each entry is a property key with its deserialization callback.
        """
        return {
            "id": lambda o,n: o.set_id(n.get_uuid_value()),
            "display_name": lambda o,n: o.set_display_name(n.get_string_value()),
            "office_location": lambda o,n: o.set_office_location(n.get_enum_value(OfficeLocation)),
            "updated_at": lambda o,n: o.set_updated_at(n.get_datetime_offset_value()),
            "birthday": lambda o,n: o.set_birthday(n.get_date_value()),
            "business_phones": lambda o,n: o.set_business_phones(n.get_collection_of_primitive_values()),
            "mobile_phone": lambda o,n: o.set_mobile_phone(n.get_string_value()),
            "is_active": lambda o,n: o.set_is_active(n.get_boolean_value()),
            "age": lambda o,n: o.set_age(n.get_int_value()),
            "gpa": lambda o,n: o.set_gpa(n.get_float_value())
        }
        
        
    def serialize(self, writer: SerializationWriter) -> None:
        """Writes the objects properties to the current writer.

        Args:
            writer (SerializationWriter): The writer to write to.
        """
        if not writer:
            raise Exception("Writer cannot be undefined")
        writer.write_string_value("id", self.get_id())
        writer.write_string_value("display_name", self.get_display_name())
        writer.write_string_value("office_location", self.get_office_location())
        writer.write_string_value("updated_at", self.get_updated_at())
        writer.write_string_value("birthday", self.get_birthday())
        writer.write_string_value("business_phones", self.get_business_phones())
        writer.write_string_value("mobile_phone", self.get_mobile_phone())
        writer.write_string_value("is_active", self.get_is_active())
        writer.write_string_value("age", self.get_age())
        writer.write_string_value("gpa", self.get_gpa())
        
        
    
    
