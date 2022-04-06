<?php

namespace Microsoft\Kiota\Abstractions\Serialization;

interface ParsableFactory {
    /**
     * Create a new parsable object from the given serialized data.
     * @param ParseNode $parseNode The node to parse use to get the discriminator value from the payload.
     * @returns Parsable The parsable object.
     */
    public function create(ParseNode $parseNode): Parsable;
}