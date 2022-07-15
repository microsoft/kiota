from abc import ABC, abstractmethod
from typing import TYPE_CHECKING, Any, Callable, Dict, TypeVar

T = TypeVar("T")

if TYPE_CHECKING:
    from .parse_node import ParseNode
    from .serialization_writer import SerializationWriter


class Parsable(ABC):
    """
    Defines a serializable model object.
    """

    @abstractmethod
    def get_field_deserializers(self) -> Dict[str, Callable[['ParseNode'], None]]:
        """Gets the deserialization information for this object.

        Returns:
            Dict[str, Callable[[ParseNode], None]]: The deserialization information for this
            object where each entry is a property key with its deserialization callback.
        """
        pass

    @abstractmethod
    def serialize(self, writer: 'SerializationWriter') -> None:
        """Writes the objects properties to the current writer.

        Args:
            writer (SerializationWriter): The writer to write to.
        """
        pass
