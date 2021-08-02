<?php

namespace Microsoft\Kiota\Abstractions;

use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactoryInterface;
use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactoryRegistry;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactoryInterface;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactoryRegistry;
use Microsoft\Kiota\Abstractions\Store\BackingStoreAbstractParseNodeFactory;
use Microsoft\Kiota\Abstractions\Store\BackingStoreSerializationWriterProxyFactory;

class ApiClientBuilder {
    private function __construct(){}

    /**
     * Registers the default serializer to the registry.
     * @param SerializationWriterFactoryInterface $factoryClass the class of the factory to be registered.
     */
    public static function registerDefaultSerializer(SerializationWriterFactoryInterface $factoryClass): void {
        $factory =  new (get_class($factoryClass))();
        SerializationWriterFactoryRegistry::getDefaultInstance()
            ->contentTypeAssociatedFactories[$factory->getValidContentType()] = $factory;
    }

    /**
     * Registers the default deserializer to the registry.
     * @param ParseNodeFactoryInterface $factoryClass the class of the factory to be registered.
     */
    public static function registerDefaultDeserializer(ParseNodeFactoryInterface $factoryClass): void {

    }

    /**
     * Enables the backing store on default serialization writers and the given serialization writer.
     * @param SerializationWriterFactoryInterface $original The serialization writer to enable the backing store on.
     * @return SerializationWriterFactoryInterface A new serialization writer with the backing store enabled.
     */
    public static function enableBackingStoreForSerializationWriterFactory(SerializationWriterFactoryInterface $original): SerializationWriterFactoryInterface {
        $result = $original;

        if (is_a($original, SerializationWriterFactoryRegistry::class)) {
            self::enableBackingStoreForSerializationWriterRegistry($original);
        } else {
            $result = new BackingStoreSerializationWriterProxyFactory($original);
        }
        self::enableBackingStoreForSerializationWriterRegistry(SerializationWriterFactoryRegistry::getDefaultInstance());
        return $result;
    }

    /**
     * Enables the backing store on default parse node factories and the given parse node factory.
     * @param ParseNodeFactoryInterface $original The parse node factory to enable the backing store on.
     * @return ParseNodeFactoryInterface A new parse node factory with the backing store enabled.
     */
    public static function enableBackingStoreForParseNodeFactory(ParseNodeFactoryInterface $original): ParseNodeFactoryInterface {
        $result = $original;
        if (is_a($original, ParseNodeFactoryRegistry::class)) {
            self::enableBackingStoreForParseNodeRegistry($original);
        } else {
            $result = new BackingStoreAbstractParseNodeFactory($original);
        }
        self::enableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry::getDefaultInstance());
        return $result;
    }

    private static function enableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry $registry): void {
        foreach (array_values($registry->contentTypeAssociatedFactories) as $factory){
            if (!is_a($factory, BackingStoreAbstractParseNodeFactory::class) && !is_a($factory, ParseNodeFactoryRegistry::class)) {
                $registry->contentTypeAssociatedFactories[$factory->getValidContentType()] = new BackingStoreAbstractParseNodeFactory($factory);
            }
        }
    }

    private static function enableBackingStoreForSerializationWriterRegistry(SerializationWriterFactoryRegistry $registry): void {
        $factories = array_values($registry->contentTypeAssociatedFactories);

        foreach ($factories as $factory) {
            if (!is_a($factory, BackingStoreSerializationWriterProxyFactory::class) &&
             !is_a($factory, SerializationWriterFactoryRegistry::class)) {
                $registry->contentTypeAssociatedFactories[$factory->getValidContentType()] = new BackingStoreSerializationWriterProxyFactory($factory);
            }
        }
    }
}
