/** Represents a middleware option. */
export interface MiddlewareOption {
    /** Gets the option key for when adding it to a request. Must be unique. */
    getKey(): string;
}