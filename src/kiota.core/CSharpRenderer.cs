using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace kiota.core
{
    public class CSharpRenderer : ILanguageRenderer
    {
        public bool AddSuffix { get; set; } = true;
        public bool InnerClasses { get; set; } = false;
        
        public bool SingleFile { get; set; } = false;

        private GenerationConfiguration config;

        public void Render(RequestBuilder root, GenerationConfiguration config)
        {
            this.config = config;
            var clientName = config.ClientClassName;
                       
            var writer = CreateNewFile(config.ClientClassName);
            RenderRequestBuilder(writer, root, clientName);
            CloseFile(writer);

            if (!this.SingleFile)
            {
                // Get list of RequestBuilders
                var requestBuilders = new List<RequestBuilder>();
                GetAllChildRequestBuilders(root,requestBuilders);

                foreach (var requestBuilder in requestBuilders)
                {
                    writer = CreateNewFile(requestBuilder.Identifier + "_" + requestBuilder.Hash());
                    RenderRequestBuilder(writer,requestBuilder, requestBuilder.Identifier);
                    CloseFile(writer);
                }
            }
        }

        private void GetAllChildRequestBuilders(RequestBuilder root, List<RequestBuilder> builders)
        {
            foreach(var rb in root.Children)
            {
                builders.Add(rb.Value);
                GetAllChildRequestBuilders(rb.Value, builders);
            }
        }

        private static void CloseFile(StreamWriter writer)
        {
            writer.WriteLine($"}} ");
            writer.Flush();
            writer.Close();
        }

        private StreamWriter CreateNewFile(string className)
        {
            string outputPath = Path.Combine(this.config.OutputPath, className + ".cs");
            var outfile = new FileStream(outputPath, FileMode.Create);

            var writer = new StreamWriter(outfile);
            writer.WriteLine($"using System;");
            writer.WriteLine($"namespace OpenApiClient {{ ");
            return writer;
        }

        private void RenderRequestBuilder(TextWriter writer, RequestBuilder node, string identifier = "")
        {
            string className;
            if (node.Path == "")
            {
                className = identifier;
            }
            else
            {
                className = $"{identifier}RequestBuilder" + (this.AddSuffix ? "_" + node.Hash() : "");
            }
            writer.WriteLine($"public class {className} {{ ");

            foreach (var child in node.Children)
            {
                var relatedRequestBuilder = child.Value.Identifier + "RequestBuilder" + (this.AddSuffix ? "_" + child.Value.Hash() : "");
                if (child.Value.IsParameter())
                {
                    writer.WriteLine($"    public {relatedRequestBuilder} this[string {child.Value.Identifier}] {{get {{ return null; }} }}");
                }
                else if (child.Value.IsFunction())
                {
                    // Don't support functions for the moment
                }
                else
                {
                    writer.WriteLine($"    public {relatedRequestBuilder} {child.Value.Identifier} {{get;}}");
                }
            }

            if (node.HasOperations())
            {
                RenderQueryParameters(writer, node.QueryParameters);
                RenderQueryBuilder(writer, node);
                RenderToRequest(writer, node);
            }

            if (!this.InnerClasses) writer.WriteLine($"}}");

            if (this.SingleFile)
            {
                foreach (var child in node.Children)
                {
                    if (!child.Value.IsFunction())
                    {
                        RenderRequestBuilder(writer, child.Value, child.Value.Identifier);
                    }
                }
            }

            if (this.InnerClasses) writer.WriteLine($"}}");

        }

        private static void RenderQueryParameters(TextWriter writer, TypedQueryParameters parameters)
        {

            writer.WriteLine(@"public class QueryParameters {");
            if (parameters != null)
            {
                foreach (var parameter in parameters.Parameters)
                {
                    writer.Write(@"public ");
                    writer.Write(ToCSharpType(parameter.Schema) + " ");
                    writer.Write(parameter.Name);
                    writer.WriteLine(@" { get; set; }");
                }
            }
            writer.WriteLine(@"}");
        }

        private static string ToCSharpType(Microsoft.OpenApi.Models.OpenApiSchema schema)
        {
            if (schema.Type == "array")
            {
                return "string[]";
            }
            else
            {
                return "string";
            }
        }

        private static void RenderQueryBuilder(TextWriter writer, RequestBuilder node)
        {
            writer.WriteLine(@"            public class QueryBuilder
            {
                private readonly QueryParameters qParams;
                public QueryBuilder(QueryParameters qParams) { this.qParams = qParams; }
");
            foreach (var item in node.PathItem.Operations)
            {
                writer.WriteLine($"          public object {item.Key.ToString()}Async() {{ return null; }}");
            }
            writer.WriteLine(@"}");
        }

        private static void RenderToRequest(TextWriter writer, RequestBuilder node)
        {
            writer.WriteLine($"    public QueryBuilder ToRequest(Action<QueryParameters> configure) {{  ");
            writer.WriteLine($"    var qParams = new QueryParameters();  ");
            writer.WriteLine($"    configure(qParams); ");
            writer.WriteLine($"    return new QueryBuilder(qParams);  }} ");
        }
    }

}
