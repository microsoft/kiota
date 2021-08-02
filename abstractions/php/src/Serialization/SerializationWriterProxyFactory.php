<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Closure;

abstract class SerializationWriterProxyFactory implements SerializationWriterFactoryInterface {

    /**
     * @var SerializationWriterFactoryInterface
     */
    private SerializationWriterFactoryInterface $concrete;

    /**
     * @var ?Closure $onBefore
     */
    private ?Closure $onBefore;
    /**
     * @var ?Closure $onAfter
     */
    private ?Closure $onAfter;

    /**
     * SerializationWriterProxyFactory constructor.
     * @param SerializationWriterFactoryInterface $concrete
     * @param ?Closure $onBefore
     * @param ?Closure $onAfter
     */
    public function __construct(SerializationWriterFactoryInterface $concrete, ?Closure $onBefore = null, ?Closure $onAfter = null) {
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
            $this->onBefore->call($x, $x);
            $originalBefore->call($x, $x);
        };
        $writer->onAfterObjectSerialization = function (AbstractParsable $x) use ($originalAfter) {
            $this->onAfter->call($x, $x);
            $originalAfter->call($x, $x);
        };
        return $writer;
    }

    public function getValidContentType(): string {
        return $this->concrete->getValidContentType();
    }
}
