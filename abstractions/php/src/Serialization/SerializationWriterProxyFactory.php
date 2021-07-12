<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Closure;

abstract class SerializationWriterProxyFactory implements SerializationWriterFactory {

    /**
     * @var SerializationWriterFactory
     */
    private SerializationWriterFactory $concrete;

    /**
     * @var Closure
     */
    private Closure $onBefore;
    /**
     * @var Closure
     */
    private Closure $onAfter;

    /**
     * SerializationWriterProxyFactory constructor.
     * @param SerializationWriterFactory $concrete
     * @param Closure $onBefore
     * @param Closure $onAfter
     */
    public function __construct(SerializationWriterFactory $concrete, Closure $onBefore, Closure $onAfter) {
        $this->concrete = $concrete;
        $this->onBefore = $onBefore;
        $this->onAfter = $onAfter;
    }

    /**
     * @param string $contentType
     * @return SerializationWriter
     */
    public function getSerializationWriter(string $contentType): SerializationWriter {
        $writer = $this->concrete->getSerializationWriter($contentType);
        $originalBefore = $writer->onBeforeObjectSerialization;
        $originalAfter  = $writer->onAfterObjectSerialization;

        $writer->onBeforeObjectSerialization = function (Parsable $x) use ($originalBefore) {
            $this->onBefore->bindTo($x);
            $originalBefore->bindTo($x);
        };
        $writer->onAfterObjectSerialization = function (Parsable $x) use ($originalAfter) {
            $this->onAfter->bindTo($x);
            $originalAfter->bindTo($x);
        };
        return $writer;
    }
}
