from typing import Callable

from ..serialization import Parsable, ParseNodeFactory, ParseNodeProxyFactory
from .backed_model import BackedModel


class BackingStoreParseNodeFactory(ParseNodeProxyFactory):
    """Proxy implementation of ParseNodeFactory for the backing store that automatically sets the
    state of the backing store when deserializing.
    """
    def __init__(
        self, concrete: ParseNodeFactory, on_before: Callable[[Parsable], None],
        on_after: Callable[[Parsable], None]
    ) -> None:
        super().__init__(concrete, on_before, on_after)
