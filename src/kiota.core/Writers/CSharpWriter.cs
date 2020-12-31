using System.Linq;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class CSharpWriter : LanguageWriter
    {
        public CSharpWriter(string rootPath, string clientNamespaceName)
        {
            segmenter = new CSharpPathSegmenter(rootPath, clientNamespaceName);
        }
        private readonly IPathSegmenter segmenter;
        public override IPathSegmenter PathSegmenter => segmenter;

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            foreach (var codeUsing in code.Usings)
            {
                WriteLine($"using {codeUsing.Name};");
            }
            if(code?.Parent?.Parent is CodeNamespace) {
                WriteLine($"namespace {code.Parent.Parent.Name} {{");
                IncreaseIndent();
            }

            WriteLine($"public class {code.Name} {{");
            IncreaseIndent();
        }

        public override void WriteCodeClassEnd(CodeClass.End code)
        {
            DecreaseIndent();
            WriteLine("}");
            if(code?.Parent?.Parent is CodeNamespace) {
                DecreaseIndent();
                WriteLine("}");
            }
        }

        public override void WriteProperty(CodeProperty code)
        {

            WriteLine($"public {GetTypeString(code.Type)} {code.Name} {{get;}}");
        }

        public override void WriteIndexer(CodeIndexer code)
        {
            WriteLine($"public {GetTypeString(code.ReturnType)} this[{GetTypeString(code.IndexType)} {code.Name}] {{get {{ return null; }} }}");
        }

        public override void WriteMethod(CodeMethod code)
        {
            WriteLine($"public Task<{GetTypeString(code.ReturnType)}> {code.Name}({string.Join(',', code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) {{ return null; }}");
        }

        public override void WriteType(CodeType code)
        {
            Write(GetTypeString(code), includeIndent: false);

        }

        public override string GetTypeString(CodeType code)
        {
            var typeName = TranslateType(code.Name, code.Schema);
            if (code.ActionOf)
            {
                return $"Action<{typeName}>";
            }
            else
            {
                return typeName;
            }
        }

        public override string TranslateType(string typeName, OpenApiSchema schema)
        {
            switch (typeName)
            {
                case "integer": typeName = "int"; break;
                case "boolean": return "bool"; 
                case "array":
                    typeName = TranslateType(schema.Items.Type, schema.Items) + "[]";
                    break;
            }

            return typeName;
        }

        public override string GetParameterSignature(CodeParameter parameter)
        {
            var parameterType = GetTypeString(parameter.Type);
            return $"{parameterType} {parameter.Name}{(parameter.Optional ? $" = default({parameterType})": string.Empty)}";
        }
    }
}
