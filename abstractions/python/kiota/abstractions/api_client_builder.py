from .serialization import (
    ParseNodeFactory,
    ParseNodeFactoryRegistry,
    SerializationWriterFactory,
    SerializationWriterFactoryRegistry,
    SerializationWriterProxyFactory,
)
from .store import BackingStoreParseNodeFactory, BackingStoreSerializationWriterProxyFactory


def register_default_serializer(factory_class: SerializationWriterFactory) -> None:
    """Registers the default serializer to the registry.

    Args:
        factory_class (SerializationWriterFactory):the class of the factory to be registered.
    """
    base_class = type(factory_class)
    serializer = base_class()
    SerializationWriterFactoryRegistry().CONTENT_TYPE_ASSOCIATED_FACTORIES[
        serializer.get_valid_content_type()] = serializer


def register_default_deserializer(self, factory_class: ParseNodeFactory) -> None:
    """Registers the default deserializer to the registry.

    Args:
        factory_class (ParseNodeFactory):the class of the factory to be registered.
    """
    base_class = type(factory_class)
    deserializer = base_class()
    ParseNodeFactoryRegistry().CONTENT_TYPE_ASSOCIATED_FACTORIES[
        deserializer.get_valid_content_type()] = deserializer


def enable_backing_store_for_serialization_writer_factory(
    original: SerializationWriterFactory
) -> SerializationWriterFactory:
    """Enables the backing store on default serialization writers and the given serialization
    writer.

    Args:
        original (SerializationWriterFactory):The serialization writer to enable the backing
        store on.
    Returns:
        SerializationWriterFactory: A new serialization writer with the backing store enabled.
    """
    result = original
    if isinstance(original, SerializationWriterFactoryRegistry):
        enable_backing_store_for_serialization_registry(original)
    else:
        result = BackingStoreSerializationWriterProxyFactory(original)
    enable_backing_store_for_serialization_registry(SerializationWriterFactoryRegistry())
    enable_backing_store_for_parse_node_registry(ParseNodeFactoryRegistry())
    return result


def enable_backing_store_for_parse_node_factory(original: ParseNodeFactory) -> ParseNodeFactory:
    """Enables the backing store on default parse node factories and the given parse node factory.

    Args:
        original (ParseNodeFactory):The parse node factory to enable the backing store on.

    Returns:
        ParseNodeFactory: A new parse node factory with the backing store enabled.
    """
    result = original
    if isinstance(original, ParseNodeFactoryRegistry):
        enable_backing_store_for_parse_node_registry(original)
    else:
        result = BackingStoreParseNodeFactory(original)
    enable_backing_store_for_parse_node_registry(ParseNodeFactoryRegistry())
    return result


def enable_backing_store_for_parse_node_registry(registry: ParseNodeFactoryRegistry) -> None:
    for key, val in registry.CONTENT_TYPE_ASSOCIATED_FACTORIES.items():
        if not isinstance(val, (BackingStoreParseNodeFactory, ParseNodeFactoryRegistry)):
            registry.CONTENT_TYPE_ASSOCIATED_FACTORIES[key] = BackingStoreParseNodeFactory(val)


def enable_backing_store_for_serialization_registry(
    registry: SerializationWriterFactoryRegistry
) -> None:
    for key, val in registry.CONTENT_TYPE_ASSOCIATED_FACTORIES.items():
        if not isinstance(
            val, (SerializationWriterProxyFactory, SerializationWriterFactoryRegistry)
        ):
            registry.CONTENT_TYPE_ASSOCIATED_FACTORIES[
                key] = BackingStoreSerializationWriterProxyFactory(val)
