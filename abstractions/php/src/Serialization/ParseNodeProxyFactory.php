<?php


namespace Microsoft\Kiota\Abstractions\Serialization;


use Closure;
use Psr\Http\Message\StreamInterface;

abstract class ParseNodeProxyFactory implements ParseNodeFactory {
    /**
     * @var ParseNodeFactory
     */
    private ParseNodeFactory $concrete;
    /**
     * @var Closure|null
     */
    private ?Closure $onBefore;
    /**
     * @var Closure|null
     */
    private ?Closure $onAfter;

    /**
     * ParseNodeProxyFactory constructor.
     * @param ParseNodeFactory $concrete
     * @param Closure|null $onBefore
     * @param Closure|null $onAfter
     */
    public function __construct(ParseNodeFactory $concrete, ?Closure $onBefore, ?Closure $onAfter) {
        $this->concrete = $concrete;
        $this->onBefore = $onBefore;
        $this->onAfter = $onAfter;
    }

    /**
     * @param string $contentType
     * @param StreamInterface $rawResponse
     * @return ParseNode
     */
    public function getRootParseNode(string $contentType, StreamInterface $rawResponse): ParseNode {
        $node = $this->concrete->getRootParseNode($contentType, $rawResponse);
        $originalBefore = $node->onBeforeAssignFieldValues;
        $originalAfter  = $node->onAfterAssignFieldValues;

        $node->onBeforeAssignFieldValues = function (Parsable $x) use ($originalBefore) {
            if (!is_null($this->onBefore)) {
                $this->onBefore->call($x, $x);
            }
            if (!is_null($originalBefore)) {
                $originalBefore->call($x, $x);
            }
        };
        $node->onAfterAssignFieldValues = function (Parsable $x) use ($originalAfter) {
            if (!is_null($this->onAfter)) {
                $this->onAfter->call($x, $x);
            }
            if (!is_null($originalAfter)) {
                $originalAfter->call($x, $x);
            }
        };
        return $node;
    }
}
