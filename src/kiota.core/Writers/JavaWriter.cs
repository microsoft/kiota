using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class JavaWriter : LanguageWriter
    {
        public override string GetFileSuffix() => ".java";

        public override string GetParameterSignature(CodeParameter parameter)
        {
            return $"@javax.annotation.Nonnull final {GetTypeString(parameter.Type)} {parameter.Name}";
        }

        public override string GetTypeString(CodeType code)
        {
            var typeName = TranslateType(code.Name, code.Schema);
            if (code.ActionOf)
            {
                return $"java.util.function.Consumer<{typeName}>";
            }
            else
            {
                return typeName;
            }
        }

        public override string TranslateType(string typeName, OpenApiSchema schema)
        {
            switch (typeName)
            {//TODO we're probably missing a bunch of type mappings
                case "integer": return "Integer";
                case "boolean": return "Boolean";
                case "string": return "String";
                case "object": return "Object";
                case "array": return $"{TranslateType(schema.Items.Type, schema.Items)}[]";
            }

            return typeName;
        }

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            //TODO: missing javadoc
            WriteLine($"public class {code.Name} {{");
            IncreaseIndent();
        }

        public override void WriteCodeClassEnd(CodeClass.End code)
        {
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteIndexer(CodeIndexer code)
        {
            WriteMethod(new CodeMethod {
                Name = "get",
                Parameters = new List<CodeParameter> {
                    new CodeParameter {
                        Name = "position",
                        Type = code.IndexType,
                        Optional = false,
                    }
                },
                ReturnType = code.IndexType
            });
        }

        public override void WriteMethod(CodeMethod code)
        {
            //TODO javadoc
            WriteLine("@javax.annotation.Nonnull");
            WriteLine($"public java.util.concurrent.Future<{GetTypeString(code.ReturnType)}> {code.Name}({string.Join(',', code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) {{ return null; }}");
        }

        public override void WriteNamespaceDeclaration(CodeNamespace.BlockDeclaration code)
        {
            WriteLine($"package {code.Name};");
            WriteLine();
            foreach (var codeUsing in code.Usings)
            {
                WriteLine($"import {codeUsing.Name}.*;");
            }
        }

        public override void WriteNamespaceEnd(CodeNamespace.BlockEnd code) => WriteLine();

        public override void WriteProperty(CodeProperty code)
        {
            //TODO: missing javadoc
            WriteLine("@javax.annotation.Nullable");
            WriteLine($"public {GetTypeString(code.Type)} {code.Name};");
        }

        public override void WriteType(CodeType code)
        {
            Write(GetTypeString(code), includeIndent: false);
        }
    }
}
