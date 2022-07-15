from abc import ABC, abstractmethod

from .serialization_writer import SerializationWriter


class SerializationWriterFactory(ABC):
    """Defines the contract for a factory that creates SerializationWriter instances.
    """

    @abstractmethod
    def get_valid_content_type(self) -> str:
        """Gets the content type this factory creates serialization writers for.

        Returns:
            str: the content type this factory creates serialization writers for.
        """

    @abstractmethod
    def get_serialization_writer(self, content_type: str) -> SerializationWriter:
        """Creates a new SerializationWriter instance for the given content type.

        Args:
            content_type (str): the content type to create a serialization writer for.

        Returns:
            SerializationWriter: A new SerializationWriter instance for the given content type.
        """
