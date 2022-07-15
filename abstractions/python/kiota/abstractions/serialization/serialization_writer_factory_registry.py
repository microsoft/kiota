import re
from typing import Dict

from .serialization_writer import SerializationWriter
from .serialization_writer_factory import SerializationWriterFactory


class SerializationWriterFactoryRegistry(SerializationWriterFactory):
    """This factory holds a list of all the registered factories for the various types of nodes.
    """
    # List of factories that are registered by content type.
    CONTENT_TYPE_ASSOCIATED_FACTORIES: Dict[str, SerializationWriterFactory] = {}

    __instance = None

    def __new__(cls, *args, **kwargs):
        """Default singleton instance of the registry to be used when registring new
        factories that should be available by default.

        Returns:
            [SerializationWriterFactoryRegistry]: Default singleton instance of the class
        """
        if not SerializationWriterFactoryRegistry.__instance:
            SerializationWriterFactoryRegistry.__instance = object.__new__(cls)
        return SerializationWriterFactoryRegistry.__instance

    def get_valid_content_type(self) -> str:
        raise Exception(
            "The registry supports multiple content types. Get the registered factory instead"
        )

    def get_serialization_writer(self, content_type: str) -> SerializationWriter:
        if not content_type:
            raise Exception("Content type cannot be null")

        vendor_specific_content_type = content_type.split(';')[0]
        factory = self.CONTENT_TYPE_ASSOCIATED_FACTORIES.get(vendor_specific_content_type)
        if factory:
            return factory.get_serialization_writer(vendor_specific_content_type)
        cleaned_content_type = re.sub(r'[^/]+\+', '', vendor_specific_content_type)

        factory = self.CONTENT_TYPE_ASSOCIATED_FACTORIES.get(cleaned_content_type)
        if factory:
            return factory.get_serialization_writer(cleaned_content_type)
        raise Exception(
            f"Content type {cleaned_content_type} does not have a factory registered"
            "to be serialized"
        )
