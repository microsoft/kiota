from ..serialization import SerializationWriterFactory, SerializationWriterProxyFactory
from .backed_model import BackedModel


class BackingStoreSerializationWriterProxyFactory(SerializationWriterProxyFactory):
    """Proxy implementation of SerializationWriterFactory for the backing store that
    automatically sets the state of the backing store when serializing.
    """
    def __init__(self) -> None:
        super().__init__()
