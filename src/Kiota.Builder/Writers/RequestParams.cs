namespace Kiota.Builder.Writers {
    public record RequestParams(CodeParameter requestBody, CodeParameter requestConfiguration);
    public record RequestProperties(CodeParameter paramSet, CodeProperty requestBody, CodeProperty queryString, CodeProperty headers, CodeProperty options);
}
