namespace Kiota.Builder.Writers {
    public record RequestParams(CodeParameter requestBody, CodeParameter queryString, CodeParameter headers, CodeParameter options);
}
