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
    private ?Closure $onStart;

    /**
     * SerializationWriterProxyFactory constructor.
     * @param SerializationWriterFactory $concrete
     * @param ?Closure $onBefore
     * @param ?Closure $onAfter
     * @param Closure|null $onStart
     */
    public function __construct(SerializationWriterFactory $concrete, ?Closure $onBefore = null, ?Closure $onAfter = null, ?Closure $onStart = null) {
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
        $originalBefore = $writer->onBeforeObjectSerialization;
        $originalAfter  = $writer->onAfterObjectSerialization;
        $originalStart = $writer->onStartObjectSerialization;

        $writer->onBeforeObjectSerialization = function (Parsable $x) use ($originalBefore) {
            $this->onBefore->call($x, $x);
            $originalBefore->call($x, $x);
        };
        $writer->onAfterObjectSerialization = function (Parsable $x) use ($originalAfter) {
            $this->onAfter->call($x, $x);
            $originalAfter->call($x, $x);
        };

        $writer->onStartObjectSerialization = function (Parsable $x, SerializationWriter $y) use ($originalStart) {
            if ($this->onStart !== null) {
                $this->onStart->call($x, $x, $y);
            }

            if ($originalStart !== null) {
                $originalStart->call($x, $x, $y);
            }
        };
        return $writer;
    }

    public function getValidContentType(): string {
        return $this->concrete->getValidContentType();
    }
}
