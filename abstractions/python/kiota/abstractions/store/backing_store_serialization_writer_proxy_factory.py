from ..serialization import SerializationWriterFactory, SerializationWriterProxyFactory
from .backed_model import BackedModel


class BackingStoreSerializationWriterProxyFactory(SerializationWriterProxyFactory):
    """Proxy implementation of SerializationWriterFactory for the backing store that
    automatically sets the state of the backing store when serializing.
    """

    def __init__(self, concrete: SerializationWriterFactory) -> None:
        """Initializes a new instance of the BackingStoreSerializationWriterProxyFactory class
        given a concrete implementation of SerializationWriterFactory.

        Args:
            concrete (SerializationWriterFactory):  a concrete implementation of
            SerializationWriterFactory to wrap.
        """

        def func1(x):
            if isinstance(x, BackedModel):
                backed_model = x
                backing_store = backed_model.get_backing_store()
                if backing_store:
                    backing_store.set_return_only_changed_values(True)

        def func2(x):
            if isinstance(x, BackedModel):
                backed_model = x
                backing_store = backed_model.get_backing_store()
                if backing_store:
                    backing_store.set_return_only_changed_values(False)
                    backing_store.set_is_initialization_completed(True)

        def func3(x, y):
            if isinstance(x, BackedModel):
                backed_model = x
                backing_store = backed_model.get_backing_store()
                if backing_store:
                    keys = backing_store.enumerate_keys_for_values_changed_to_null()
                    for key in keys:
                        y.write_null_value(key)

        super().__init__(concrete, func1, func2, func3)
