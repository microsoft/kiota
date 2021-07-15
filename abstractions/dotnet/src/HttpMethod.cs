namespace Microsoft.Kiota.Abstractions {
    /// <summary>
    ///     Represents the HTTP method used by a request.
    /// </summary>
    public enum HttpMethod {
        /// <summary>
        ///     The HTTP GET method.
        /// </summary>
        GET,
        /// <summary>
        ///     The HTTP POST method.
        /// </summary>
        POST,
        /// <summary>
        ///     The HTTP PATCH method.
        /// </summary>
        PATCH,
        /// <summary>
        ///     The HTTP DELETE method.
        /// </summary>
        DELETE,
        /// <summary>
        ///     The HTTP OPTIONS method.
        /// </summary>
        OPTIONS,
        /// <summary>
        ///     The HTTP PUT method.
        /// </summary>
        PUT,
        /// <summary>
        ///     The HTTP HEAD method.
        /// </summary>
        HEAD,
        /// <summary>
        ///     The HTTP CONNECT method.
        /// </summary>
        CONNECT,
        /// <summary>
        ///     The HTTP TRACE method.
        /// </summary>
        TRACE
    }
}
