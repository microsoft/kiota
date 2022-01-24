from io import BytesIO
from typing import Callable

from .parsable import Parsable
from .parse_node import ParseNode
from .parse_node_factory import ParseNodeFactory


class ParseNodeProxyFactory(ParseNodeFactory):
    """Proxy factory that allows the composition of before and after callbacks on existing factories
    """
    def __init__(
        self, concrete: ParseNodeFactory, on_before: Callable[[Parsable], None],
        on_after: Callable[[Parsable], None]
    ) -> None:
        """Creates a new proxy factory that wraps the specified concrete factory while composing
        the before and after callbacks.

        Args:
            concrete (ParseNodeFactory): The concrete factory to wrap.
            on_before (Callable[[Parsable], None]): The callback to invoke before the
            deserialization
            of any model object.
            on_after (Callable[[Parsable], None]): The callback to invoke after the deserialization
            of any model object.
        """
        self._concrete = concrete
        self._on_before = on_before
        self._on_after = on_after

    def get_valid_content_type(self) -> str:
        """
        Returns:
            str: The valid content type for the ParseNodeFactory instance
        """
        return super().get_valid_content_type()

    def get_root_parse_node(self, content_type: str, content: BytesIO) -> ParseNode:
        node = self._concrete.get_root_parse_node(content_type, content)
        original_before = node.on_before_assign_field_values

        return super().get_root_parse_node(content_type, content)
