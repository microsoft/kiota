/** Parent interface for errors thrown by the client when receiving failed responses to its requests. */
export class ApiError extends Error {
  public constructor(message?: string) {
    super(message);
  }
}
