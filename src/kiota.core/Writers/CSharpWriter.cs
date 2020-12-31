using System.Linq;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class CSharpWriter : LanguageWriter
    {

        public override void WriteNamespaceEnd(CodeNamespace.BlockEnd code)
        {
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteNamespaceDeclaration(CodeNamespace.BlockDeclaration code)
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
            var simpleBody = "get;";
            if (!code.ReadOnly)
            {
                simpleBody = "get;set;";
            }
            var defaultValue = string.Empty;
            if (code.DefaultValue != null)
            {
                defaultValue = " = " + code.DefaultValue + ";";
            }
            WriteLine($"public {GetTypeString(code.Type)} {code.Name} {{{simpleBody}}}{defaultValue}");
        }

        public override void WriteIndexer(CodeIndexer code)
        {
            WriteLine($"public {GetTypeString(code.ReturnType)} this[{GetTypeString(code.IndexType)} {code.Name}] {{get {{ return null; }} }}");
        }

        public override void WriteMethod(CodeMethod code)
        {
            var staticModifier = code.IsStatic ? "static " : string.Empty;
            // Task type should be moved into the refiner
            WriteLine($"public {staticModifier}Task<{GetTypeString(code.ReturnType)}> {code.Name}({string.Join(',', code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) {{ return null; }}");

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

        public override string GetFileSuffix()
        {
            return ".cs";
        }
    }
}
