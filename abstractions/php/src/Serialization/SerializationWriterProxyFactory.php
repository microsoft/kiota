<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Closure;

/**
 * Proxy factory that allows the composition of before and after callbacks on existing factories.
 * @method onBefore(Parsable $x)
 * @method onAfter(Parsable $x)
 * @method onStart(Parsable $x, SerializationWriter $y)
 */
abstract class SerializationWriterProxyFactory implements SerializationWriterFactory {

    /**
     * @var SerializationWriterFactory
     */
    private SerializationWriterFactory $concrete;

    /**
     * @var callable|null $onBefore
     */
    private  $onBefore;
    /**
     * @var callable|null $onAfter
     */
    private  $onAfter;

    /**
     * @var callable|null
     */
    private  $onStart;

    /**
     * Creates a new proxy factory that wraps the specified concrete factory while composing the before and after callbacks.
     * @param SerializationWriterFactory $concrete the concrete factory to wrap
     * @param callable|null $onBefore the callback to invoke before the serialization of any model object.
     * @param callable|null $onAfter the callback to invoke after the serialization of any model object.
     * @param callable|null $onStart the callback to invoke when the serialization of a model object starts.
     */
    public function __construct(SerializationWriterFactory $concrete, ?callable $onBefore = null, ?callable $onAfter = null, ?callable $onStart = null) {
        $this->concrete = $concrete;
        $this->onBefore = $onBefore;
        $this->onAfter = $onAfter;
        $this->onStart = $onStart;
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

        $writer->setOnBeforeObjectSerialization(function (Parsable $x) use ($originalBefore) {
            if ($this->onBefore !== null) {
                $this->onBefore($x);  // the callback set by the implementation (e.g. backing store)
            }

            if ($originalBefore !== null) {
                $originalBefore($x); // some callback that might already be set on the target
            }
        });
        $writer->setOnAfterObjectSerialization(function (Parsable $x) use ($originalAfter) {

            if ($this->onAfter !== null) {
                $this->onAfter($x);
            }

            if ($originalAfter !== null) {
                $originalAfter($x);
            }
        });

        $writer->setOnStartObjectSerialization(function (Parsable $x, SerializationWriter $y) use ($originalStart) {
            if ($this->onStart !== null) {
                $this->onStart($x, $y);
            }

            if ($originalStart !== null) {
                $originalStart($x, $y);
            }
        });
        return $writer;
    }

    public function getValidContentType(): string {
        return $this->concrete->getValidContentType();
    }
}
