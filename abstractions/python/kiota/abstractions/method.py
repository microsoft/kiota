from enum import Enum


class Method(Enum):
    """Represents the HTTP method used by a request."""
    # The HTTP GET method
    GET = "GET"
    # The HTTP POST method
    POST = "POST"
    # The HTTP PATCH method
    PATCH = "PATCH"
    # The HTTP DELETE method
    DELETE = "DELETE"
    # The HTTP OPTIONS method
    OPTIONS = "OPTIONS"
    # The HTTP CONNECT method
    CONNECT = "CONNECT"
    # The HTTP TRACE method
    TRACE = "TRACE"
    # The HTTP HEAD method
    HEAD = "HEAD"
    # The HTTP PUT method
    PUT = "PUT"
