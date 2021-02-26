using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace kiota.core
{
    public class JavaWriter : LanguageWriter
    {
        private readonly IPathSegmenter segmenter;

        public JavaWriter(string rootPath, string clientNamespaceName)
        {
            segmenter = new JavaPathSegmenter(rootPath, clientNamespaceName);
        }
        public override IPathSegmenter PathSegmenter => segmenter;

        public override string GetParameterSignature(CodeParameter parameter)
        {
            return $"@javax.annotation.{(parameter.Optional ? "Nullable" : "Nonnull")} final {GetTypeString(parameter.Type)} {parameter.Name}";
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
                default: return typeName ?? "Object";
            }
        }

        public override void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
            if(code?.Parent?.Parent is CodeNamespace ns) {
                WriteLine($"package {ns.Name};");
                WriteLine();
                code.Usings
                    .Select(x => $"import {x.Name}.{x.Declaration.Name.ToFirstCharacterUpperCase()};")
                    .Distinct()
                    .ToList()
                    .ForEach(x => WriteLine(x));
            }
            //TODO: missing javadoc
            WriteLine($"public class {code.Name.ToFirstCharacterUpperCase()} {{");
            IncreaseIndent();
        }

        public override void WriteCodeClassEnd(CodeClass.End code)
        {
            DecreaseIndent();
            WriteLine("}");
        }

        public override void WriteIndexer(CodeIndexer code)
        {
            throw new InvalidOperationException("indexers are not supported in Java, the refiner should have replaced those by methods");
        }

        public override void WriteMethod(CodeMethod code)
        {
            //TODO javadoc
            WriteLine(code.ReturnType.IsNullable ? "@javax.annotation.Nullable" : "@javax.annotation.Nonnull");
            WriteLine($"{GetAccessModifier(code.Access)} {(code.IsAsync ? "java.util.concurrent.CompletableFuture<" : string.Empty)}{GetTypeString(code.ReturnType).ToFirstCharacterUpperCase()}{(code.IsAsync ? ">" : string.Empty)} {code.Name.ToFirstCharacterLowerCase()}({string.Join(',', code.Parameters.Select(p=> GetParameterSignature(p)).ToList())}) {{");
            IncreaseIndent();
            foreach(var parameter in code.Parameters.Where(x => !x.Type.IsNullable)) {
                WriteLine($"Objects.requireNonNull({parameter.Name});");
            }
            switch(code.MethodKind) {
                case CodeMethodKind.IndexerBackwardCompatibility:
                    var pathSegment = code.GenerationProperties.ContainsKey(pathSegmentPropertyName) ? code.GenerationProperties[pathSegmentPropertyName] as string : string.Empty;
                    var returnType = GetTypeString(code.ReturnType);
                    AddRequestBuilderBody(returnType, $" + \"/{(string.IsNullOrEmpty(pathSegment) ? string.Empty : pathSegment + "/" )}\" + id");
                break;
                default:
                    WriteLine("return null;");
                break;
            }
            DecreaseIndent();
            WriteLine("}");
        }
        private const string pathSegmentPropertyName = "pathSegment";
        private const string currentPathPropertyName = "currentPath";
        private void AddRequestBuilderBody(string returnType, string suffix = default) {
            // we're assigning this temp variable because java doesn't have a way to differentiate references with same names in properties initializers
            // and because if currentPath is null it'll add "null" to the string...
            WriteLine($"final String parentPath = ({currentPathPropertyName} == null ? \"\" : {currentPathPropertyName}) + {pathSegmentPropertyName}{suffix};");
            WriteLine($"return new {returnType}() {{{{ {currentPathPropertyName} = parentPath; }}}};");
        }
        public override void WriteProperty(CodeProperty code)
        {
            //TODO: missing javadoc
            var returnType = GetTypeString(code.Type);
            switch(code.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    WriteLine("@javax.annotation.Nonnull");
                    WriteLine($"{GetAccessModifier(code.Access)} {returnType} {code.Name.ToFirstCharacterLowerCase()}() {{");
                    IncreaseIndent();
                    AddRequestBuilderBody(returnType);
                    DecreaseIndent();
                    WriteLine("}");
                break;
                default:
                    var defaultValue = string.IsNullOrEmpty(code.DefaultValue) ? string.Empty : $" = {code.DefaultValue}";
                    WriteLine(code.Type.IsNullable ? "@javax.annotation.Nullable" : "@javax.annotation.Nonnull");
                    WriteLine($"{GetAccessModifier(code.Access)}{(code.ReadOnly ? " final " : " ")}{returnType} {code.Name.ToFirstCharacterLowerCase()}{defaultValue};");
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
