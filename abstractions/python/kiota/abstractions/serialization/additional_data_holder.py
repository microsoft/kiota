from abc import ABC, abstractmethod
from typing import Any, Dict


class AdditionalDataHolder(ABC):
    """Defines a contract for models that can hold additional data besides the described properties.
    """

    @property
    @abstractmethod
    def additional_data(self) -> Dict[str, Any]:
        """Stores the additional data for this object that did not belong to the properties.

        Returns:
            Dict[str, Any]: The additional data for this object
        """
        pass
