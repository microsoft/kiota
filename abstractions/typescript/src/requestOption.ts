/** Represents a request option. */
export interface RequestOption {
  /** Gets the option key for when adding it to a request. Must be unique. */
  getKey(): string;
}
