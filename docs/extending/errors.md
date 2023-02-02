---
parent: Kiota deep dive
---

# Error handling

## Error types mapping

The abstractions library for each language defined an API Exception type (or error) which inherits or implements the default error/exception type for the language.

Kiota will also generate types for schemas mapped to [400-599[ status codes, as well as 4XX and 5XX ranges, and make them derive from the API exception defined in the abstractions library.

> Note: there currently is a limitation at generation type, if the error schema is an allOf, kiota will error as most languages do not support multiple parents inheritance.

This mapping of codes to types will be passed to the request adapter as a dictionary/map so it can be used at runtime and is specific to each endpoint.

> Note: the default response is ignored as it is ambiguous to whether it represents a successful or failed response description.

## Runtime behavior

At runtime, if an error status code is returned by the API, the request adapter will follow this sequence:

1. If a mapping is present for the specific status code and if the response body can be deserialized to that type, deserialize and throw.
1. If a mapping is present for the corresponding range (4XX, 5XX) and if the response body can be deserialized to that type, deserialize and throw.
1. Otherwise throw a new instance of the API exception defined in the abstractions library.

The generated clients throw exceptions/errors when running into failed response codes to allow the consumer to tell the difference between a successful response that didn't return a body (204, etc...) and a failed response. The consumer can choose to implement try/catch behavior that either catches the generic API exception type or is more specific to an exception type mapped to a range or even a status code depending on the scenario.

## Transient errors

Most request adapters handle transient errors (e.g. 429 because of throttling, network interruption...) through two main mechanisms. When transient errors are handled and requests are retried, the request adapter itself and the generated client are *not aware* that an error happened and consequently won't throw any exception.

- The native clients of most platforms provide support for some transient errors handling (e.g. reconnecting when the network was interrupted)
- Request adapters provide a middleware infrastructure to implement cross-cutting concerns like transient error handling (e.g. exponential back-off when throttled) which is enabled by default.
