using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class TypeScriptWriter : LanguageWriter
    {
        public TypeScriptWriter(string rootPath, string clientNamespaceName)
        {
            segmenter = new TypeScriptPathSegmenter(rootPath,clientNamespaceName);
        }
        private readonly IPathSegmenter segmenter;
        public override IPathSegmenter PathSegmenter => segmenter;
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
                var relativeImportPath = GetRelativeImportPathForUsing(codeUsing, code.GetImmediateParentOfType<CodeNamespace>());
                                                    
                WriteLine($"import {{{codeUsing.Name}}} from '{relativeImportPath}{(string.IsNullOrEmpty(relativeImportPath) ? codeUsing.Name : codeUsing.Name.ToFirstCharacterLowerCase())}';");
            }
            WriteLine();
            WriteLine($"export class {code.Name} {{");
            IncreaseIndent();
        }
        private string GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace) {
            if(codeUsing.Declaration == null)
                return string.Empty;//it's an external import, add nothing
            var typeDef = codeUsing.Declaration.TypeDefinition;
            if(typeDef == null) {
                // sometimes the definition is not attached to the declaration because it's generated after the fact, we need to search it
                typeDef = currentNamespace
                    .GetRootNamespace()
                    .GetChildElementOfType<CodeClass>(x => x.Name.Equals(codeUsing.Declaration.Name));
            }

            if(typeDef == null)
                return "./"; // it's relative to the folder, with no declaration (default failsafe)
            else
                return GetImportRelativePathFromNamespaces(currentNamespace, 
                                                        typeDef.GetImmediateParentOfType<CodeNamespace>());
        }
        private static char namespaceNameSeparator = '.';
        private string GetImportRelativePathFromNamespaces(CodeNamespace currentNamespace, CodeNamespace importNamespace) {
            if(currentNamespace == null)
                throw new ArgumentNullException(nameof(currentNamespace));
            else if (importNamespace == null)
                throw new ArgumentNullException(nameof(importNamespace));
            else if(currentNamespace.Name.Equals(importNamespace.Name)) // we're in the same namespace
                return "./";
            else {
                var currentNamespaceSegements = currentNamespace
                                    .Name
                                    .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
                var importNamespaceSegments = importNamespace
                                    .Name
                                    .Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
                var importNamespaceSegmentsCount = importNamespaceSegments.Count();
                var currentNamespaceSegementsCount = currentNamespaceSegements.Count();
                var deeperMostSegmentIndex = 0;
                while(deeperMostSegmentIndex < Math.Min(importNamespaceSegmentsCount, currentNamespaceSegementsCount)) {
                    if(currentNamespaceSegements.ElementAt(deeperMostSegmentIndex).Equals(importNamespaceSegments.ElementAt(deeperMostSegmentIndex)))
                        deeperMostSegmentIndex++;
                    else
                        break;
                }
                if(importNamespaceSegmentsCount > currentNamespaceSegementsCount) { // we're in a parent namespace and need to import with a relative path
                    return "./" + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
                } else { // we're in a sub namespace and need to go "up" with dot dots
                    var upMoves = currentNamespaceSegementsCount - importNamespaceSegmentsCount;
                    var upMovesBuilder = new StringBuilder();
                    for(var i = 0; i < upMoves; i++)
                        upMovesBuilder.Append("../");
                    return upMovesBuilder.ToString() + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
                }
            }
        }
        private string GetRemainingImportPath(IEnumerable<string> remainingSegments) {
            if(remainingSegments.Any())
                return remainingSegments.Aggregate((x, y) => $"{x}/{y}") + '/';
            else
                return string.Empty;
        }

        public override void WriteCodeClassEnd(CodeClass.End code)
        {
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteIndexer(CodeIndexer code)
        {
            var method = new CodeMethod(code) {
                Name = "item",
                ReturnType = code.IndexType
            };
            method.AddParameter(new CodeParameter(method) {
                        Name = "position",
                        Type = code.IndexType,
                        Optional = false,
                    });
            WriteMethod(method);
        }

        public override void WriteMethod(CodeMethod code)
        {
            WriteLine($"public readonly {code.Name.ToFirstCharacterLowerCase()} = ({string.Join(',', code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) : Promise<{GetTypeString(code.ReturnType)}> => {{ return Promise.resolve({(code.ReturnType.Name.Equals("string") ? "''" : "{}")}); }}");
        }

        public override void WriteProperty(CodeProperty code)
        {
            WriteLine($"public {code.Name}?: {GetTypeString(code.Type)} | undefined;");
        }

        public override void WriteType(CodeType code)
        {
            Write(GetTypeString(code), includeIndent: false);
        }
    }
}
