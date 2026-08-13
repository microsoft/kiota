using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

public record RequestParams(CodeParameter? requestBody, CodeParameter? requestConfiguration, CodeParameter? requestContentType)
{
    public CodeProperty? Headers => requestConfiguration?.GetHeadersProperty();
    public CodeProperty? QueryParameters => requestConfiguration?.GetQueryProperty();
    public CodeProperty? Options => requestConfiguration?.GetOptionsProperty();
}
