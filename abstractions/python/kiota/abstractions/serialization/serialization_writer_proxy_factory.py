from typing import Callable, Optional

from .parsable import Parsable
from .serialization_writer import SerializationWriter
from .serialization_writer_factory import SerializationWriterFactory


class SerializationWriterProxyFactory(SerializationWriterFactory):
    """Proxy factory that allows the composition of before and after callbacks on existing factories
    """

    def __init__(
        self, concrete: SerializationWriterFactory, on_before: Optional[Callable[[Parsable], None]],
        on_after: Optional[Callable[[Parsable], None]],
        on_start: Optional[Callable[[Parsable, SerializationWriter], None]]
    ) -> None:
        """Creates a new proxy factory that wraps the specified concrete factory while composing
        the before and after callbacks.

        Args:
            concrete (SerializationWriterFactory): the concrete factory to wrap
            on_before (Optional[Callable[[Parsable], None]]): the callback to invoke before the
            serialization of any model object.
            on_after (Optional[Callable[[Parsable], None]]): the callback to invoke after the
            serialization of any model object.
            on_start (Optional[Callable[[Parsable, SerializationWriter], None]]): the callback to
            invoke when the serialization of a model object starts
        """
        self._concrete = concrete
        self._on_before = on_before
        self._on_after = on_after
        self._on_start = on_start

    def get_valid_content_type(self) -> str:
        """
        Returns:
            str: The valid content type for the ParseNodeFactory instance
        """
        return self._concrete.get_valid_content_type()

    def get_serialization_writer(self, content_type: str) -> SerializationWriter:
        """Creates a new SerializationWriter instance for the given content type.

        Args:
            content_type (str): the content type to create a serialization writer for.

        Returns:
            SerializationWriter: A new SerializationWriter instance for the given content type.
        """
        writer = self._concrete.get_serialization_writer(content_type)
        original_before = writer.get_on_before_object_serialization()
        original_after = writer.get_on_after_object_serialization()
        original_start = writer.get_on_start_object_serialization()

        def before_callback(value):
            if self._on_before:
                self._on_before(value)
            if original_before:
                original_before(value)

        writer.set_on_before_object_serialization(before_callback)

        def after_callback(value):
            if self._on_after:
                self._on_after(value)
            if original_after:
                original_after(value)

        writer.set_on_after_object_serialization(after_callback)

        def start_callback(value1, value2):
            if self._on_start:
                self._on_start(value1, value2)
            if original_start:
                original_start(value1, value2)

        writer.set_on_start_object_serialization(start_callback)

        return writer
