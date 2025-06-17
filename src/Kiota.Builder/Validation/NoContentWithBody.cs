using Microsoft.OpenApi;

namespace Kiota.Builder.Validation;

public class NoContentWithBody : ValidationRule<OpenApiOperation>
{
    public NoContentWithBody() : base(nameof(NoContentWithBody), static (context, operation) =>
    {
        if (operation.Responses is not null && operation.Responses.TryGetValue("204", out var response) && response.Content is { Count: > 0 })
            context.CreateWarning(nameof(NoContentWithBody), "A 204 response with a body media type was found. The response body will be ignored.");
    })
    {
    }
}
