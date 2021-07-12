<?php
namespace Microsoft\Kiota\Abstractions;

final class HttpMethod {
    public const GET = "GET";
    public const POST = "POST";
    public const PATCH = "PATCH";
    public const DELETE = "DELETE";
    public const OPTIONS = "OPTIONS";
    public const CONNECT = "CONNECT";
    public const TRACE = "TRACE";
    public const HEAD = "HEAD";
    public const PUT = "PUT";

    /**
     * This should never be initialized
     * HttpMethod constructor.
     */
    private function __construct(){}
}
