using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class GoConventionService : CommonLanguageConventionService
    {
        public override string StreamTypeName => "[]byte";

        public override string VoidTypeName => string.Empty;

        public override string DocCommentPrefix => string.Empty;

        private const string PathSegmentPropertyName = "pathSegment";
        private const string CurrentPathPropertyName = "currentPath";
        private const string HttpCorePropertyName = "requestAdapter";
        public override string ParseNodeInterfaceName => "ParseNode";
        #pragma warning disable CA1822 // Method should be static
        public string AbstractionsHash => "ida96af0f171bb75f894a4013a6b3146a4397c58f11adb81a2b7cbea9314783a9";
        #pragma warning restore CA1822 // Method should be static
        public override string GetAccessModifier(AccessModifier access)
        {
            throw new InvalidOperationException("go uses a naming convention for access modifiers");
        }
        public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement)
        {
            return $"{parameter.Name} {GetTypeString(parameter.Type, targetElement)}";
        }
        public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true) =>
            GetTypeString(code, targetElement, includeCollectionInformation, true);
        public string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation, bool addPointerSymbol)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"Go does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var importSymbol = GetImportSymbol(code, targetElement);
                if(!string.IsNullOrEmpty(importSymbol))
                    importSymbol += ".";
                var typeName = TranslateType(currentType, true);
                var nullableSymbol = addPointerSymbol && 
                                    currentType.IsNullable &&
                                    currentType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None &&
                                    !IsScalarType(currentType.Name) ? "*"
                                    : string.Empty;
                var collectionPrefix = currentType.CollectionKind switch {
                    CodeType.CodeTypeCollectionKind.Array or CodeType.CodeTypeCollectionKind.Complex when includeCollectionInformation => "[]",
                    _ => string.Empty,
                };
                if (currentType.ActionOf)
                    return $"func (value {nullableSymbol}{collectionPrefix}{importSymbol}{typeName}) (err error)";
                else
                    return $"{nullableSymbol}{collectionPrefix}{importSymbol}{typeName}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }

        public override string TranslateType(CodeType type) => throw new InvalidOperationException("use the overload instead.");
        #pragma warning disable CA1822 // Method should be static
        public string TranslateType(CodeTypeBase type, bool includeImportSymbol)
        {
            if(type.Name.StartsWith("map[")) return type.Name; //casing hack

            return type.Name switch {
                "void" => string.Empty,
                "float" => "float32",
                "integer" => "int32",
                "long" => "int64",
                "double" => "float64",
                "boolean" => "bool",
                "guid" when includeImportSymbol => "uuid.UUID",
                "guid" when !includeImportSymbol => "UUID",
                "DateTimeOffset" or "Time" when includeImportSymbol => "i336074805fc853987abe6f7fe3ad97a6a6f3077a16391fec744f671a015fbd7e.Time",
                "DateTimeOffset" or "Time" when !includeImportSymbol => "Time",
                "binary" => "[]byte",
                "string" or "float32" or "float64" or "int32" or "int64" => type.Name,
                "String" or "Int64" or "Int32" or "Float32" or "Float64" => type.Name.ToFirstCharacterLowerCase(), //casing hack
                _ => type.Name.ToFirstCharacterUpperCase() ?? "Object",
            };
        }
        public bool IsPrimitiveType(string typeName) {
            return typeName.TrimCollectionAndPointerSymbols() switch {
                "void" or "string" or "float" or "integer" or "long" or "double" or "boolean" or "guid" or "DateTimeOffset"
                or "bool" or "int32" or "int64" or "float32" or "float64" or "UUID" or "Time" => true,
                _ => false,
            };
        }
        public bool IsScalarType(string typeName) {
            if(typeName.StartsWith("map[")) return true;
            return typeName.ToLowerInvariant() switch {
                "binary" or "void" or "[]byte" => true,
                _ => false,
            };
        }
        #pragma warning restore CA1822 // Method should be static
        private string GetImportSymbol(CodeTypeBase currentBaseType, CodeElement targetElement) {
            if(currentBaseType == null || IsPrimitiveType(currentBaseType.Name)) return string.Empty;
            var targetNamespace = targetElement.GetImmediateParentOfType<CodeNamespace>();
            if(currentBaseType is CodeType currentType) {
                if(currentType.TypeDefinition is CodeClass currentClassDefinition &&
                   currentClassDefinition.Parent is CodeNamespace classNS &&
                   targetNamespace != classNS)
                       return classNS.GetNamespaceImportSymbol();
                else if(currentType.TypeDefinition is CodeEnum currentEnumDefinition &&
                   currentEnumDefinition.Parent is CodeNamespace enumNS &&
                   targetNamespace != enumNS)
                       return enumNS.GetNamespaceImportSymbol();
                else if(currentType.TypeDefinition is null &&
                        targetElement is CodeClass targetClass) {
                            var symbolUsing = (targetClass.Parent is CodeClass parentClass ? parentClass : targetClass)
                                                            .StartBlock
                                                            .Usings
                                                            .FirstOrDefault(x => currentBaseType.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                            return symbolUsing == null ? string.Empty : symbolUsing.Declaration.Name.GetNamespaceImportSymbol();
                        }
            }
            return string.Empty;
        }

        public override void WriteShortDescription(string description, LanguageWriter writer)
        {
            throw new NotImplementedException();
        }
        #pragma warning disable CA1822 // Method should be static
        internal void AddRequestBuilderBody(bool addCurrentPath, string returnType, LanguageWriter writer, string suffix = default, IEnumerable<CodeParameter> pathParameters = default)
        {
            var currentPath = addCurrentPath ? $"m.{CurrentPathPropertyName} + " : string.Empty;
            var splatImport = returnType.Split('.');
            var constructorName = splatImport.Last().TrimCollectionAndPointerSymbols().ToFirstCharacterUpperCase();
            var moduleName = splatImport.Length > 1 ? $"{splatImport.First()}." : string.Empty;
            var pathParametersSuffix = !(pathParameters?.Any() ?? false) ? string.Empty : $"{string.Join(", ", pathParameters.Select(x => $"{x.Name}"))}, ";
            writer.WriteLines($"return *{moduleName}New{constructorName}({currentPath}m.{PathSegmentPropertyName}{suffix}, m.{HttpCorePropertyName}, {pathParametersSuffix}false);");
        }
        #pragma warning restore CA1822 // Method should be static
    }
}
