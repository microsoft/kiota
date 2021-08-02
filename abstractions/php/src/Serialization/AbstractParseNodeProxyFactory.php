<?php


namespace Microsoft\Kiota\Abstractions\Serialization;


use Closure;
use Psr\Http\Message\StreamInterface;

abstract class AbstractParseNodeProxyFactory implements ParseNodeFactoryInterface {
    /**
     * @var ParseNodeFactoryInterface
     */
    private ParseNodeFactoryInterface $concrete;
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
     * @param ParseNodeFactoryInterface $concrete
     * @param Closure|null $onBefore
     * @param Closure|null $onAfter
     */
    public function __construct(ParseNodeFactoryInterface $concrete, ?Closure $onBefore, ?Closure $onAfter) {
        $this->concrete = $concrete;
        $this->onBefore = $onBefore;
        $this->onAfter = $onAfter;
    }

    /**
     * @param string $contentType
     * @param StreamInterface $rawResponse
     * @return AbstractParseNode
     */
    public function getParseNode(string $contentType, StreamInterface $rawResponse): AbstractParseNode {
        $node = $this->concrete->getParseNode($contentType, $rawResponse);
        $originalBefore = $node->onBeforeAssignFieldValues;
        $originalAfter  = $node->onAfterAssignFieldValues;

        $node->onBeforeAssignFieldValues = function (AbstractParsable $x) use ($originalBefore) {
            if (!is_null($this->onBefore)) {
                $this->onBefore->call($x, $x);
            }
            if (!is_null($originalBefore)) {
                $originalBefore->call($x, $x);
            }
        };
        $node->onAfterAssignFieldValues = function (AbstractParsable $x) use ($originalAfter) {
            if (!is_null($this->onAfter)) {
                $this->onAfter->call($x, $x);
            }
            if (!is_null($originalAfter)) {
                $originalAfter->call($x, $x);
            }
        };
        return $node;
    }

    public function getValidContentType(): string {
        return $this->concrete->getValidContentType();
    }
}
