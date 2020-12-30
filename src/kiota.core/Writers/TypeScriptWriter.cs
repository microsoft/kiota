using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class TypeScriptWriter : LanguageWriter
    {
        public override string GetFileSuffix() => ".ts";

        public override string GetParameterSignature(CodeParameter parameter)
        {
            return $"{parameter.Name}{(parameter.Optional ? "?" : string.Empty)}: {GetTypeString(parameter.Type)}{(parameter.Optional ? " | undefined": string.Empty)}";
        }

        public override string GetTypeString(CodeType code)
        {
            var typeName = TranslateType(code.Name, code.Schema);
            if (code.ActionOf)
            {
                IncreaseIndent(5);
                var childElements = code.TypeDefinition
                                            .InnerChildElements
                                            .OfType<CodeProperty>()
                                            .Select(x => $"{x.Name}?: {GetTypeString(x.Type)}");
                var innerDeclaration = childElements.Any() ? 
                                                NewLine +
                                                GetIndent() +
                                                childElements
                                                .Aggregate((x, y) => $"{x};{NewLine}{GetIndent()}{y}")
                                                .Replace(';', ',') +
                                                NewLine +
                                                GetIndent()
                                            : string.Empty;
                DecreaseIndent();
                return $"{{{innerDeclaration}}}";
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
                case "integer": return "number";
                case "array": return $"{TranslateType(schema.Items.Type, schema.Items)}[]";
            } // string, boolean, object : same casing

            return typeName;
        }

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            foreach (var codeUsing in code.Usings)
            {
                WriteLine($"import {{{codeUsing.Name}}} from './{codeUsing.Name}';");
            }
            WriteLine();
            WriteLine($"export class {code.Name} {{");
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
                Name = "item",
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
            WriteLine($"public readonly {code.Name.ToFirstCharacterLowerCase()} = ({string.Join(',', code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) : Promise<{GetTypeString(code.ReturnType)}> => {{ return Promise.resolve({(code.ReturnType.Name.Equals("string") ? "''" : "{}")}); }}");
        }

        public override void WriteNamespaceDeclaration(CodeNamespace.BlockDeclaration code) => WriteLine();

        public override void WriteNamespaceEnd(CodeNamespace.BlockEnd code) => WriteLine();

        public override void WriteProperty(CodeProperty code)
        {
            WriteLine($"public {code.Name}?: {GetTypeString(code.Type)}");
        }

        public override void WriteType(CodeType code)
        {
            Write(GetTypeString(code), includeIndent: false);
        }
    }
}
