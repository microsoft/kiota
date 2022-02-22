<?php

namespace Microsoft\Kiota\Abstractions;

use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactory;
use Microsoft\Kiota\Abstractions\Serialization\ParseNodeFactoryRegistry;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactory;
use Microsoft\Kiota\Abstractions\Serialization\SerializationWriterFactoryRegistry;
use Microsoft\Kiota\Abstractions\Store\BackingStoreParseNodeFactory;
use Microsoft\Kiota\Abstractions\Store\BackingStoreSerializationWriterProxyFactory;
use ReflectionClass;
use ReflectionException;
use RuntimeException;
use InvalidArgumentException;

class ApiClientBuilder {
    private function __construct(){}

    /**
     * Registers the default serializer to the registry.
     * @param string $factoryClass the class of the factory to be registered.
     */
    public static function registerDefaultSerializer(string $factoryClass): void {
        if (!is_subclass_of($factoryClass, SerializationWriterFactory::class)) {
             throw new InvalidArgumentException('The class passed must be a subclass of SerializationWriterFactory::class');
        }
        try {
            $reflectionClass = new ReflectionClass($factoryClass);

            /** @var SerializationWriterFactory $factory */
            $factory = $reflectionClass->newInstance();
            SerializationWriterFactoryRegistry::getDefaultInstance()
                ->contentTypeAssociatedFactories[$factory->getValidContentType()] = $factory;
        } catch (ReflectionException $exception){
            throw new RuntimeException($exception);
        }
    }

    /**
     * Registers the default deserializer to the registry.
     * @param string $factoryClass the class of the factory to be registered.
     */
    public static function registerDefaultDeserializer(string $factoryClass): void {
        if (!is_subclass_of($factoryClass, ParseNodeFactory::class)) {
             throw new InvalidArgumentException('The class passed must be a subclass of ParseNodeFactory::class');
        }
        try {
            $reflectionClass = new ReflectionClass($factoryClass);
            /**
             * @var ParseNodeFactoryRegistry $factory
             */
            $factory = $reflectionClass->newInstance();
            ParseNodeFactoryRegistry::getDefaultInstance()
                ->contentTypeAssociatedFactories[$factory->getValidContentType()] = $factory;
        } catch (ReflectionException $exception) {
            throw new RuntimeException($exception);
        }
    }

    /**
     * Enables the backing store on default serialization writers and the given serialization writer.
     * @param SerializationWriterFactory $original The serialization writer to enable the backing store on.
     * @return SerializationWriterFactory A new serialization writer with the backing store enabled.
     */
    public static function enableBackingStoreForSerializationWriterFactory(SerializationWriterFactory $original): SerializationWriterFactory {
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
     * @param ParseNodeFactory $original The parse node factory to enable the backing store on.
     * @return ParseNodeFactory A new parse node factory with the backing store enabled.
     */
    public static function enableBackingStoreForParseNodeFactory(ParseNodeFactory $original): ParseNodeFactory {
        $result = $original;
        if (is_a($original, ParseNodeFactoryRegistry::class)) {
            self::enableBackingStoreForParseNodeRegistry($original);
        } else {
            $result = new BackingStoreParseNodeFactory($original);
        }
        self::enableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry::getDefaultInstance());
        return $result;
    }

    /**
     * @param ParseNodeFactoryRegistry $registry
     */
    private static function enableBackingStoreForParseNodeRegistry(ParseNodeFactoryRegistry $registry): void {
        foreach ($registry->contentTypeAssociatedFactories as $factory){
            if (!is_a($factory, BackingStoreParseNodeFactory::class) && !is_a($factory, ParseNodeFactoryRegistry::class)) {
                $registry->contentTypeAssociatedFactories[$factory->getValidContentType()] = new BackingStoreParseNodeFactory($factory);
            }
        }
    }

    /**
     * @param SerializationWriterFactoryRegistry $registry
     */
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
