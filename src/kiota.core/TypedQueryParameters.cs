using System.Collections.Generic;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class TypedQueryParameters
    {
        public List<OpenApiParameter> Parameters { get; set; } = new List<OpenApiParameter>();
    }

}
