from typing import Optional

from .parsable import Parsable
from .parse_node import ParseNode, U


class ParsableFactory(Parsable):
    """Defines the factory for creating parsable objects.
    """

    @staticmethod
    def create_from_discriminator_value(parse_node: Optional[ParseNode]) -> U:
        """Create a new parsable object from the given serialized data.

        Args:
            parse_node (Optional[ParseNode]): The node to parse to get the discriminator value
            from the payload.

        Returns:
            U: The parsable object.
        """
        pass
