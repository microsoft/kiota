<?php
namespace Microsoft\Kiota\Abstractions\Serialization;

use Closure;

abstract class SerializationWriterProxyFactory implements SerializationWriterFactory {

    /**
     * @var SerializationWriterFactory
     */
    private SerializationWriterFactory $concrete;

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
     * @param SerializationWriterFactory $concrete
     * @param ?Closure $onBefore
     * @param ?Closure $onAfter
     */
    public function __construct(SerializationWriterFactory $concrete, ?Closure $onBefore = null, ?Closure $onAfter = null) {
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
            $this->onBefore->call($x, $x);
            $originalBefore->call($x, $x);
        };
        $writer->onAfterObjectSerialization = function (Parsable $x) use ($originalAfter) {
            $this->onAfter->call($x, $x);
            $originalAfter->call($x, $x);
        };
        return $writer;
    }

    public function getValidContentType(): string {
        return $this->concrete->getValidContentType();
    }
}
