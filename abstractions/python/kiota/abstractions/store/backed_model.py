from abc import ABC, abstractmethod

from .backing_store import BackingStore


class BackedModel(ABC):
    """Defines the contracts for a model that is backed by a store.
    """

    @abstractmethod
    def get_backing_store(self) -> BackingStore:
        """Gets the store that is backing the model

        Returns:
            BackingStore: The store backing the model
        """
        pass
