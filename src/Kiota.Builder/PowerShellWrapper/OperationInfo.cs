using System.Collections.Generic;

namespace Kiota.Builder.PowerShellWrapper;

public sealed record OperationInfo(
    string HttpMethod,
    string Path,
    string OperationId,
    IReadOnlyList<string> PathParams,
    IReadOnlyList<string> QueryParams,
    bool HasBody,
    // True when the success response wraps results in a "value" array (GET /users) rather
    // than returning a single entity (GET /users/{id}). Path-param count alone cannot tell
    // these apart for nested resources: a nested list and a nested get-by-id both carry the
    // parent's path param.
    bool IsCollectionResponse = false,
    // Raw OpenAPI header parameter names, for example "If-Match". Graph sometimes requires
    // these even where the spec marks them optional (Planner's PATCH/DELETE), so they become
    // real cmdlet parameters instead of being dropped.
    IReadOnlyList<string>? HeaderParams = null);
