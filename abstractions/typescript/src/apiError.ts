/** Parent interface for errors thrown by the client when receiving failed responses to its requests. */
interface ApiError extends Error {
}

interface ApiErrorConstructor extends ErrorConstructor {
    new(message?: string): ApiError;
    (message?: string): ApiError;
    readonly prototype: ApiError;
}

export var ApiError: ApiErrorConstructor;