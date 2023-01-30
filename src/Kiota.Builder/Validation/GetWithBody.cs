using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Kiota.Builder.Validation;

public class GetWithBody : ValidationRule<OpenApiPathItem>
{
    public GetWithBody() : base(static (context, pathItem) =>
    {
        if (pathItem.Operations.TryGetValue(OperationType.Get, out var getOperation) && getOperation.RequestBody != null)
            context.CreateWarning(nameof(GetWithBody), "A GET operation with a body was found. The request body will be ignored.");
    })
    {
    }
}
