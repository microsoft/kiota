namespace Kiota.Builder.Writers;
public record RequestParams(CodeParameter requestBody, CodeParameter requestConfiguration)
{
    public CodeProperty Headers => requestConfiguration.GetHeadersProperty();
    public CodeProperty QueryParameters => requestConfiguration.GetQueryProperty();
    public CodeProperty Options => requestConfiguration.GetOptionsProperty();
}
