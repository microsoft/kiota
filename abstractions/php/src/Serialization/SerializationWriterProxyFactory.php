<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

/**
 * Proxy factory that allows the composition of before and after callbacks on existing factories.
 * @method onBeforeObjectSerialization(Parsable $model)
 * @method onAfterObjectSerialization(Parsable $model)
 * @method onStartObjectSerialization(Parsable $model, SerializationWriter $serializationWriter)
 */
abstract class SerializationWriterProxyFactory implements SerializationWriterFactory {

    /**
     * @var SerializationWriterFactory
     */
    private SerializationWriterFactory $concrete;

    /**
     * @var callable|null $onBeforeObjectSerialization
     */
    private  $onBeforeObjectSerialization;
    /**
     * @var callable|null $onAfterObjectSerialization
     */
    private  $onAfterObjectSerialization;

    /**
     * @var callable|null
     */
    private  $onStartObjectSerialization;

    /**
     * Creates a new proxy factory that wraps the specified concrete factory while composing the before and after callbacks.
     * @param SerializationWriterFactory $concreteSerializationWriterFactory the concrete factory to wrap
     * @param callable|null $onBeforeObjectSerialization the callback to invoke before the serialization of any model object.
     * @param callable|null $onAfterObjectSerialization the callback to invoke after the serialization of any model object.
     * @param callable|null $onStartObjectSerialization the callback to invoke when the serialization of a model object starts.
     */
    public function __construct(SerializationWriterFactory $concreteSerializationWriterFactory, ?callable $onBeforeObjectSerialization = null, ?callable $onAfterObjectSerialization = null, ?callable $onStartObjectSerialization = null) {
        $this->concrete = $concreteSerializationWriterFactory;
        $this->onBeforeObjectSerialization = $onBeforeObjectSerialization;
        $this->onAfterObjectSerialization = $onAfterObjectSerialization;
        $this->onStartObjectSerialization = $onStartObjectSerialization;
    }

    /**
     * @param string $contentType
     * @return SerializationWriter
     */
    public function getSerializationWriter(string $contentType): SerializationWriter {
        $writer = $this->concrete->getSerializationWriter($contentType);
        $originalBefore = $writer->getOnBeforeObjectSerialization();
        $originalAfter  = $writer->getOnAfterObjectSerialization();
        $originalStart = $writer->getOnStartObjectSerialization();

        $writer->setOnBeforeObjectSerialization(function (Parsable $model) use ($originalBefore) {
            if ($this->onBeforeObjectSerialization !== null) {
                $this->onBeforeObjectSerialization($model);  // the callback set by the implementation (e.g. backing store)
            }

            if ($originalBefore !== null) {
                $originalBefore($model); // some callback that might already be set on the target
            }
        });
        $writer->setOnAfterObjectSerialization(function (Parsable $model) use ($originalAfter) {

            if ($this->onAfterObjectSerialization !== null) {
                $this->onAfterObjectSerialization($model);
            }

            if ($originalAfter !== null) {
                $originalAfter($model);
            }
        });

        $writer->setOnStartObjectSerialization(function (Parsable $model, SerializationWriter $serializationWriter) use ($originalStart) {
            if ($this->onStartObjectSerialization !== null) {
                $this->onStartObjectSerialization($model, $serializationWriter);
            }

            if ($originalStart !== null) {
                $originalStart($model, $serializationWriter);
            }
        });
        return $writer;
    }

    public function getValidContentType(): string {
        return $this->concrete->getValidContentType();
    }
}
