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
                IncreaseIndent(4);
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
                default: return typeName ?? "object";
            } // string, boolean, object : same casing
        }

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            foreach (var codeUsing in code.Usings.Where(x => !x.Declaration.Name.Equals(code.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                var relativeImportPath = GetRelativeImportPathForUsing(codeUsing, code.GetImmediateParentOfType<CodeNamespace>());
                                                    
                WriteLine($"import {{{codeUsing.Declaration?.Name ?? codeUsing.Name}}} from '{relativeImportPath}{(string.IsNullOrEmpty(relativeImportPath) ? codeUsing.Name : codeUsing.Declaration.Name.ToFirstCharacterLowerCase())}';");
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
            else if(currentNamespace.Name.Equals(importNamespace.Name, StringComparison.InvariantCultureIgnoreCase)) // we're in the same namespace
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
                    if(currentNamespaceSegements.ElementAt(deeperMostSegmentIndex).Equals(importNamespaceSegments.ElementAt(deeperMostSegmentIndex), StringComparison.InvariantCultureIgnoreCase))
                        deeperMostSegmentIndex++;
                    else
                        break;
                }
                if (deeperMostSegmentIndex == currentNamespaceSegementsCount) { // we're in a parent namespace and need to import with a relative path
                    return "./" + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
                } else { // we're in a sub namespace and need to go "up" with dot dots
                    var upMoves = currentNamespaceSegementsCount - deeperMostSegmentIndex;
                    var upMovesBuilder = new StringBuilder();
                    for(var i = 0; i < upMoves; i++)
                        upMovesBuilder.Append("../");
                    return upMovesBuilder.ToString() + GetRemainingImportPath(importNamespaceSegments.Skip(deeperMostSegmentIndex));
                }
            }
        }
        private string GetRemainingImportPath(IEnumerable<string> remainingSegments) {
            if(remainingSegments.Any())
                return remainingSegments.Select(x => x.ToFirstCharacterLowerCase()).Aggregate((x, y) => $"{x}/{y}") + '/';
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
            throw new InvalidOperationException("indexers are not supported in TypeScript, the refiner should have removes those");
        }
        private const string currentPathPropertyName = "currentPath";
        private const string pathSegmentPropertyName = "pathSegment";
        private void AddRequestBuilderBody(string returnType, string suffix = default) {
            WriteLine($"const builder = new {returnType}();");
            WriteLine($"builder.{currentPathPropertyName} = this.{currentPathPropertyName} && this.{currentPathPropertyName} + this.{pathSegmentPropertyName}{suffix};");
            WriteLine("return builder;");
        }
        public override void WriteMethod(CodeMethod code)
        {
            WriteLine($"{GetAccessModifier(code.Access)} readonly {code.Name.ToFirstCharacterLowerCase()} = ({string.Join(',', code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) : {(code.IsAsync ? "Promise<": string.Empty)}{GetTypeString(code.ReturnType)}{(code.IsAsync ? ">": string.Empty)} => {{");
            IncreaseIndent();
            switch(code.MethodKind) {
                case CodeMethodKind.IndexerBackwardCompatibility:
                    var returnType = GetTypeString(code.ReturnType);
                    var pathSegment = code.GenerationProperties.ContainsKey(pathSegmentPropertyName) ? code.GenerationProperties[pathSegmentPropertyName] as string : string.Empty;
                    AddRequestBuilderBody(returnType, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
                    break;
                default:
                    WriteLine($"return {(code.IsAsync ? "Promise.resolve(" : string.Empty)}{(code.ReturnType.Name.Equals("string") ? "''" : "{} as any")}{(code.IsAsync ? ")" : string.Empty)};");
                    break;
            }
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteProperty(CodeProperty code)
        {
            var returnType = GetTypeString(code.Type);
            switch(code.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    WriteLine($"{GetAccessModifier(code.Access)} get {code.Name.ToFirstCharacterLowerCase()}(): {returnType} {{");
                    IncreaseIndent();
                    AddRequestBuilderBody(returnType);
                    DecreaseIndent();
                    WriteLine("}");
                break;
                default:
                    var defaultValue = string.IsNullOrEmpty(code.DefaultValue) ? string.Empty : $" = {code.DefaultValue}";
                    WriteLine($"{GetAccessModifier(code.Access)}{(code.ReadOnly ? " readonly ": " ")}{code.Name.ToFirstCharacterLowerCase()}{(code.Type.IsNullable ? "?" : string.Empty)}: {returnType}{(code.Type.IsNullable ? " | undefined" : string.Empty)}{defaultValue};");
                break;
            }
        }

        public override void WriteType(CodeType code)
        {
            Write(GetTypeString(code), includeIndent: false);
        }
        public override string GetAccessModifier(AccessModifier access)
        {
            return (access == AccessModifier.Public ? "public" : (access == AccessModifier.Protected ? "protected" : "private"));
        }
    }
}
