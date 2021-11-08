namespace Kiota.Builder.Writers {
    public record RequestParams(CodeParameter requestBody, CodeParameter queryString, CodeParameter headers, CodeParameter options);
    public record RequestProperties(CodeParameter paramSet, CodeProperty requestBody, CodeProperty queryString, CodeProperty headers, CodeProperty options);
}
