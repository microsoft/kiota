/** Parent interface for errors thrown by the client when receiving failed responses to its requests. */
export class ApiError implements Error {
    public name: string;
    public message: string;
    public stack?: string;
    public constructor(message?: string) {
        this.message = message || "";
    }
}