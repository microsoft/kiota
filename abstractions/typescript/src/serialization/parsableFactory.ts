import { Parsable } from "./parsable";
import { ParseNode } from "./parseNode";

export type ParsableFactory<T extends Parsable> = (
  parseNode: ParseNode | undefined
) => T;
