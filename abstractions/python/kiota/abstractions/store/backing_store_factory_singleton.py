from .backing_store_factory import BackingStoreFactory
from .in_memory_backing_store_factory import InMemoryBackingStoreFactory


class BackingStoreFactorySingleton():

    __instance: BackingStoreFactory = InMemoryBackingStoreFactory()
