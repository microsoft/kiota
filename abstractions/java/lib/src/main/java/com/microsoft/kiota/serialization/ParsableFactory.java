package com.microsoft.kiota.serialization;

import javax.annotation.Nonnull;

/**
 * Defines the factory for creating parsable objects.
 * </summary>
 * @param T The type of the parsable object.
 */
@FunctionalInterface
public interface ParsableFactory<T extends Parsable> {
    /**
     * Create a new parsable object from the given serialized data.
     * @param parseNode The node to parse use to get the discriminator value from the payload.
     * @returns The parsable object.
     */
    T Create(@Nonnull final ParseNode parseNode);
}
