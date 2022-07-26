from .backing_store import BackingStore
from .backing_store_factory import BackingStoreFactory
from .in_memory_backing_store import InMemoryBackingStore


class InMemoryBackingStoreFactory(BackingStoreFactory):
    """This class is used to create instances of InMemoryBackingStore
    """

    def create_backing_store(self) -> BackingStore:
        return InMemoryBackingStore()
