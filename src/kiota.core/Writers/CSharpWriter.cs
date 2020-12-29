using System.Linq;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class CSharpWriter : LanguageWriter
    {

        public override void WriteNamespaceEnd(CodeNamespace.End code)
        {
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteNamespaceDeclaration(CodeNamespace.Declaration code)
        {
            foreach (var codeUsing in code.Usings)
            {
                WriteLine($"using {codeUsing.Name};");
            }
            WriteLine($"namespace {code.Name} {{");
            IncreaseIndent();
        }

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            WriteLine($"public class {code.Name} {{");
            IncreaseIndent();
        }

        public override void WriteCodeClassEnd(CodeClass.End code)
        {
            DecreaseIndent();
            WriteLine("}");
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

        private string GetTypeString(CodeType code)
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

        private static string TranslateType(string typeName, OpenApiSchema schema)
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

        private string GetParameterSignature(CodeParameter parameter)
        {
            return $"{GetTypeString(parameter.Type)} {parameter.Name}";
        }

        public override string GetFileSuffix()
        {
            return ".cs";
        }
    }
}
