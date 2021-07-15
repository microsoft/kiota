<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Closure;

abstract class SerializationWriterProxyFactory implements SerializationWriterFactoryInterface {

    /**
     * @var SerializationWriterFactoryInterface
     */
    private SerializationWriterFactoryInterface $concrete;

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
     * @param SerializationWriterFactoryInterface $concrete
     * @param Closure $onBefore
     * @param Closure $onAfter
     */
    public function __construct(SerializationWriterFactoryInterface $concrete, Closure $onBefore, Closure $onAfter) {
        $this->concrete = $concrete;
        $this->onBefore = $onBefore;
        $this->onAfter = $onAfter;
    }

    /**
     * @param string $contentType
     * @return AbstractSerializationWriter
     */
    public function getSerializationWriter(string $contentType): AbstractSerializationWriter {
        $writer = $this->concrete->getSerializationWriter($contentType);
        $originalBefore = $writer->onBeforeObjectSerialization;
        $originalAfter  = $writer->onAfterObjectSerialization;

        $writer->onBeforeObjectSerialization = function (AbstractParsable $x) use ($originalBefore) {
            $this->onBefore->bindTo($x);
            $originalBefore->bindTo($x);
        };
        $writer->onAfterObjectSerialization = function (AbstractParsable $x) use ($originalAfter) {
            $this->onAfter->bindTo($x);
            $originalAfter->bindTo($x);
        };
        return $writer;
    }
}
