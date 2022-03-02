import { Parsable } from "./parsable";
import { ParseNode } from "./parseNode";

/**
 * Defines the factory for creating parsable objects.
 * @param parseNode The node to parse use to get the discriminator value from the payload.
 * @returns The parsable object.
 */
export type ParsableFactory<T extends Parsable> = (
  parseNode: ParseNode | undefined
) => T;
