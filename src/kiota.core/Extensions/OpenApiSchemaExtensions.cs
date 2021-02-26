using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder {
    public static class OpenApiSchemaExtensions {
        public static IEnumerable<string> GetClassNames(this OpenApiSchema schema) {
            if(schema.Items != null)
                return schema.Items.GetClassNames();
            else if(schema.AnyOf.Any())
                return schema.AnyOf.Select(x => x.Title);
            else if(schema.AllOf.Any())
                return schema.AllOf.Select(x => x.Title);
            else if(schema.OneOf.Any())
                return schema.OneOf.Select(x => x.Title);
            else if(!string.IsNullOrEmpty(schema.Title))
                return new List<string>{ schema.Title };
            else return Enumerable.Empty<string>();
        }
        public static string GetClassName(this OpenApiSchema schema) {
            return schema.GetClassNames().LastOrDefault();
        }
    }
}
