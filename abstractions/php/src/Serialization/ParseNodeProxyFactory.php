<?php


namespace Microsoft\Kiota\Abstractions\Serialization;


use Psr\Http\Message\StreamInterface;

/**
 * @method onBefore(Parsable $x)
 * @method onAfter(Parsable $x)
 */
abstract class ParseNodeProxyFactory implements ParseNodeFactory {
    /**
     * @var ParseNodeFactory
     */
    private ParseNodeFactory $concrete;
    /**
     * @var callable|null
     */
    private  $onBefore;
    /**
     * @var callable|null
     */
    private $onAfter;

    /**
     * Creates a new proxy factory that wraps the specified concrete factory while composing the before and after callbacks.
     * @param ParseNodeFactory $concrete the concrete factory to wrap
     * @param callable|null $onBefore the callback to invoke before the deserialization of any model object.
     * @param callable|null $onAfter the callback to invoke after the deserialization of any model object.
     */
    public function __construct(ParseNodeFactory $concrete, ?callable $onBefore, ?callable $onAfter) {
        $this->concrete = $concrete;
        $this->onBefore = $onBefore;
        $this->onAfter = $onAfter;
    }

    /**
     * @param string $contentType
     * @param StreamInterface $rawResponse
     * @return ParseNode
     */
    public function getParseNode(string $contentType, StreamInterface $rawResponse): ParseNode {
        $node = $this->concrete->getParseNode($contentType, $rawResponse);
        $originalBefore  = $node->getOnBeforeAssignFieldValues();
        $originalAfter  = $node->getOnAfterAssignFieldValues();

        $node->setOnBeforeAssignFieldValues(function (Parsable $x) use ($originalBefore) {
            if (!is_null($this->onBefore)) {
                $this->onBefore($x);
            }
            if (!is_null($originalBefore)) {
                $originalBefore($x);
            }
        });
        $node->setOnAfterAssignFieldValues(function (Parsable $x) use ($originalAfter) {
            if (!is_null($this->onAfter)) {
                $this->onAfter($x);
            }
            if (!is_null($originalAfter)) {
                $originalAfter($x);
            }
        });
        return $node;
    }

    public function getValidContentType(): string {
        return $this->concrete->getValidContentType();
    }
}
