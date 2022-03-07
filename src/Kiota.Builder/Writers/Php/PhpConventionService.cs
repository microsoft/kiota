using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

namespace Kiota.Builder.Writers.Php
{
    public class PhpConventionService: CommonLanguageConventionService
    {
        public override string TempDictionaryVarName => "urlTplParams";

        private static CodeUsingDeclarationNameComparer _usingDeclarationNameComparer = new();

        public override string GetAccessModifier(AccessModifier access)
        {
            return (access) switch
            {
                (AccessModifier.Public) => "public",
                (AccessModifier.Protected) => "protected",
                _ => "private"
            };
        }
        public override string StreamTypeName => "StreamInterface";

        public override string VoidTypeName => "void";

        public override string DocCommentPrefix => " * ";

        private static string PathParametersPropertyName => "$pathParameters";

        private static string RequestAdapterPropertyName => "$requestAdapter";

        public override string ParseNodeInterfaceName => "ParseNode";

        public string DocCommentStart => "/**";

        public string DocCommentEnd => "*/";
        
        internal HashSet<string> PrimitiveTypes = new(StringComparer.OrdinalIgnoreCase) {"string", "boolean", "integer", "float", "date", "datetime", "time", "dateinterval", "int", "double", "decimal", "bool"};

        public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"PHP does not support union types, the union type {code.Name} should have been filtered out by the refiner.");
            if (code is CodeType currentType)
            {
                var typeName = TranslateType(currentType);
                if (!currentType.IsExternal && IsSymbolDuplicated(typeName, targetElement))
                {
                    return $"{MakeNamespaceAliasVariable(currentType.TypeDefinition.GetImmediateParentOfType<CodeNamespace>().Name.ToFirstCharacterUpperCase())}{typeName.ToFirstCharacterUpperCase()}";
                }
            }
            return code is {IsCollection: true} ? "array" : TranslateType(code);
        }

        public override string TranslateType(CodeType type)
        {
            string typeName = type.Name;
            return (typeName.ToLowerInvariant()) switch
            {
                "boolean" => "bool",
                "double" or "decimal" => "float",
                "integer" or "int32" or "int64" => "int",
                "object" or "string" or "array" or "float" or "void" => typeName.ToLowerInvariant(),
                "binary" => "StreamInterface",
                _ => typeName.ToFirstCharacterUpperCase()
            };
        }

        public string GetParameterName(CodeParameter parameter)
        {
            return (parameter.Kind) switch
            {
                CodeParameterKind.Headers => "$headers",
                CodeParameterKind.Options => "$options",
                CodeParameterKind.BackingStore => "$backingStore",
                CodeParameterKind.QueryParameter => "$queryParameters",
                CodeParameterKind.PathParameters => "$pathParameters",
                CodeParameterKind.RequestAdapter => RequestAdapterPropertyName,
                CodeParameterKind.RequestBody => "$body",
                CodeParameterKind.RawUrl => "$rawUrl",
                CodeParameterKind.Serializer => "$writer",
                CodeParameterKind.ResponseHandler => "$responseHandler",
                CodeParameterKind.SetterValue => "$value",
                _ => $"${parameter.Name.ToFirstCharacterLowerCase()}"
            };
        }
        public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement)
        {
            if (parameter?.Type == null)
            {
                return string.Empty;
            }
            var typeString = GetTypeString(parameter?.Type, parameter);
            var methodTarget = targetElement as CodeMethod;
            var parameterSuffix = parameter?.Kind switch
            {
                CodeParameterKind.RequestAdapter => $"RequestAdapter {GetParameterName(parameter)}",
                CodeParameterKind.ResponseHandler => $"ResponseHandler {GetParameterName(parameter)}",
                CodeParameterKind.QueryParameter => $"array {GetParameterName(parameter)}",
                CodeParameterKind.Serializer => $"SerializationWriter {GetParameterName(parameter)}",
                CodeParameterKind.BackingStore => $"BackingStore {GetParameterName(parameter)}",
                _ => $"{typeString} {GetParameterName(parameter)}"

            };
            var qualified = parameter?.Optional != null && parameter.Optional &&
                            (methodTarget != null && !methodTarget.IsOfKind(CodeMethodKind.Setter));
            return parameter?.Optional != null && parameter.Optional ? $"?{parameterSuffix} {(qualified ?  "= null" : string.Empty)}" : parameterSuffix;
        }
        public string GetParameterDocNullable(CodeParameter parameter, CodeElement codeElement)
        {
            var parameterSignature = GetParameterSignature(parameter, codeElement).Trim().Split(' ');
            if (parameter.IsOfKind(CodeParameterKind.PathParameters, CodeParameterKind.Headers))
            {
                return $"array<string, mixed>{(parameter.Optional ? "|null": string.Empty)} {parameterSignature[1]}";
            } else if (parameter.IsOfKind(CodeParameterKind.Options))
            {
                return $"array<string, RequestOption>|null {parameterSignature[1]}";
            }
            var isCollection = parameter.Type.IsCollection;
            var collectionDoc = isCollection ? $"array<{TranslateType(parameter.Type)}>{(parameter.Optional ? "|null" : string.Empty)} {parameterSignature[1]}" : string.Empty;
            return parameter.Optional switch
            {
                true => $"{(isCollection ? collectionDoc : $"{parameterSignature[0].Trim('?')}|null {parameterSignature[1]}")}",
                _ => isCollection ? collectionDoc : string.Join(' ', parameterSignature)
            };
        }

        private static string RemoveInvalidDescriptionCharacters(string originalDescription) => originalDescription?.Replace("\\", "/");
        public override void WriteShortDescription(string description, LanguageWriter writer)
        {
            
            if (!String.IsNullOrEmpty(description))
            {
                writer.WriteLine(DocCommentStart);
                writer.WriteLine(
                    $"{DocCommentPrefix}{RemoveInvalidDescriptionCharacters(description)}");
                writer.WriteLine(DocCommentEnd);
            }
        }

        public void AddRequestBuilderBody(string returnType, LanguageWriter writer, string suffix = default, CodeElement method = default)
        {
            var codeMethod = method as CodeMethod;
            var pathParameters = codeMethod?.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Path));
            var joined = string.Empty;
            var codeParameters = pathParameters?.ToList();
            if (pathParameters != null && codeParameters.Any())
            {
                joined = $", {string.Join(", ", codeParameters.Select(parameter => $"${parameter.Name.ToFirstCharacterLowerCase()}"))}";
            }

            writer.WriteLines($"return new {returnType}($this->{RemoveDollarSignFromPropertyName(PathParametersPropertyName)}{suffix}, $this->{RemoveDollarSignFromPropertyName(RequestAdapterPropertyName)}{joined});");
        }

        private string RemoveDollarSignFromPropertyName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || propertyName.Length < 2)
            {
                throw new ArgumentException(nameof(propertyName) + " must not be null and have at least 2 characters.");
            }
            
            return propertyName[1..];
        }

        public void WritePhpDocumentStart(LanguageWriter writer)
        {
            writer.WriteLines("<?php", string.Empty);
        }
        public void WriteNamespaceAndImports(ClassDeclaration codeElement, LanguageWriter writer)
        {
            bool hasUse = false;
            if (codeElement?.Parent?.Parent is CodeNamespace codeNamespace)
            {
                writer.WriteLine($"namespace {codeNamespace.Name.ReplaceDotsWithSlashInNamespaces()};");
                writer.WriteLine();
                codeElement.Usings?
                    .Where(x => x.Declaration != null && (x.Declaration.IsExternal ||
                                !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(x =>
                    {
                        string namespaceValue;
                        if (x.Declaration is {IsExternal: true})
                        {
                            namespaceValue = string.IsNullOrEmpty(x.Declaration.Name) ? string.Empty : $"{x.Declaration.Name.ReplaceDotsWithSlashInNamespaces()}\\";
                            return
                                $"use {namespaceValue}{x.Name.ReplaceDotsWithSlashInNamespaces()}{(!string.IsNullOrEmpty(x.Alias) ? $" as {x.Alias}" : string.Empty)};";
                        }
                        namespaceValue = string.IsNullOrEmpty(x.Name) ? string.Empty : $"{x.Name.ReplaceDotsWithSlashInNamespaces()}\\";
                            return
                                $"use {namespaceValue}{x.Declaration.Name.ReplaceDotsWithSlashInNamespaces()}{(!string.IsNullOrEmpty(x.Alias) ? $" as {x.Alias}" : string.Empty)};";
                    })
                        .Distinct()
                    .OrderBy(x => x)
                    .ToList()
                    .ForEach(x =>
                    {
                        hasUse = true;
                        writer.WriteLine(x);
                    });
            }
            if (hasUse)
            {
                writer.WriteLine(string.Empty);
            }
        }
        internal void AddRequestBuilderBody(CodeClass parentClass, string returnType, LanguageWriter writer, string urlTemplateVarName = default, IEnumerable<CodeParameter> pathParameters = default) {
            var codeParameters = pathParameters as CodeParameter[] ?? pathParameters?.ToArray();
            var codePathParametersSuffix = !(codeParameters?.Any() ?? false) ? string.Empty : $", {string.Join(", ", codeParameters.Select(x => $"{x.Name.ToFirstCharacterLowerCase()}"))}";
            var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
            var requestAdapterProp = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
            var urlTemplateParams = urlTemplateVarName ?? $"$this->{pathParametersProperty.Name}";
            writer.WriteLines($"return new {returnType}(${urlTemplateParams}, $this->{requestAdapterProp.Name}{codePathParametersSuffix});");
        }
        internal void AddParametersAssignment(LanguageWriter writer, CodeTypeBase pathParametersType, string pathParametersReference, params (CodeTypeBase, string, string)[] parameters) {
            if(pathParametersType == null) return;
            writer.WriteLine($"${TempDictionaryVarName} = {pathParametersReference};");
            if(parameters.Any())
                writer.WriteLines(parameters.Select(p => 
                    $"${TempDictionaryVarName}['{p.Item2}'] = {p.Item3};"
                ).ToArray());
        }
        
        private static bool IsSymbolDuplicated(string symbol, CodeElement targetElement) {
            var targetClass = targetElement as CodeClass ?? targetElement.GetImmediateParentOfType<CodeClass>();
            if (targetClass.Parent is CodeClass parentClass) 
                targetClass = parentClass;
            return targetClass.StartBlock
                ?.Usings
                ?.Where(x => !x.IsExternal && symbol.Equals(x.Declaration.TypeDefinition.Name, StringComparison.OrdinalIgnoreCase))
                ?.Distinct(_usingDeclarationNameComparer)
                ?.Count() > 1;
        }

        private static string MakeNamespaceAliasVariable(string name)
        {
            var parts = name.Split(new[]{'\\', '.'}, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(string.Empty, parts.Select(x => x.ToFirstCharacterUpperCase()).ToArray());
        }
    }
}
