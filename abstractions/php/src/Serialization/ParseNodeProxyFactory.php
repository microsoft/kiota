<?php


namespace Microsoft\Kiota\Abstractions\Serialization;


use Psr\Http\Message\StreamInterface;

/**
 * @method onBeforeAssignFieldValues(Parsable $model)
 * @method onAfterAssignFieldValues(Parsable $model)
 */
abstract class ParseNodeProxyFactory implements ParseNodeFactory {
    /**
     * @var ParseNodeFactory
     */
    private ParseNodeFactory $concreteParseNodeFactory;
    /**
     * @var callable|null
     */
    private $onBeforeAssignFieldValues;
    /**
     * @var callable|null
     */
    private $onAfterAssignFieldValues;

    /**
     * Creates a new proxy factory that wraps the specified concrete factory while composing the before and after callbacks.
     * @param ParseNodeFactory $concreteParseNodeFactory the concrete factory to wrap
     * @param callable|null $onBeforeAssignFieldValues the callback to invoke before the deserialization of any model object.
     * @param callable|null $onAfterAssignFieldValues the callback to invoke after the deserialization of any model object.
     */
    public function __construct(ParseNodeFactory $concreteParseNodeFactory, ?callable $onBeforeAssignFieldValues, ?callable $onAfterAssignFieldValues) {
        $this->concreteParseNodeFactory = $concreteParseNodeFactory;
        $this->onBeforeAssignFieldValues = $onBeforeAssignFieldValues;
        $this->onAfterAssignFieldValues = $onAfterAssignFieldValues;
    }

    /**
     * @param string $contentType
     * @param StreamInterface $rawResponse
     * @return ParseNode
     */
    public function getParseNode(string $contentType, StreamInterface $rawResponse): ParseNode {
        $node = $this->concreteParseNodeFactory->getParseNode($contentType, $rawResponse);
        $originalBefore  = $node->getOnBeforeAssignFieldValues();
        $originalAfter  = $node->getOnAfterAssignFieldValues();

        $node->setOnBeforeAssignFieldValues(function (Parsable $model) use ($originalBefore) {
            if (!is_null($this->onBeforeAssignFieldValues)) {
                $this->onBeforeAssignFieldValues($model);
            }
            if (!is_null($originalBefore)) {
                $originalBefore($model);
            }
        });
        $node->setOnAfterAssignFieldValues(function (Parsable $model) use ($originalAfter) {
            if (!is_null($this->onAfterAssignFieldValues)) {
                $this->onAfterAssignFieldValues($model);
            }
            if (!is_null($originalAfter)) {
                $originalAfter($model);
            }
        });
        return $node;
    }

    public function getValidContentType(): string {
        return $this->concreteParseNodeFactory->getValidContentType();
    }
}
