using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class GoConventionService : ILanguageConventionService
    {
        public string StreamTypeName => "[]byte";

        public string VoidTypeName => string.Empty;

        public string DocCommentPrefix => string.Empty;

        public string PathSegmentPropertyName => "pathSegment";

        public string CurrentPathPropertyName => "currentPath";

        public string HttpCorePropertyName => "httpCore";

        public string ParseNodeInterfaceName => "ParseNode";

        public object AbstractionsHash => "ida96af0f171bb75f894a4013a6b3146a4397c58f11adb81a2b7cbea9314783a9";
        internal string DocCommentStart = "/*";
        internal string DocCommentEnd = " */";

        public string GetAccessModifier(AccessModifier access)
        {
            throw new InvalidOperationException("go uses a naming convention for access modifiers");
        }
        public string GetParameterSignature(CodeParameter parameter) {
            throw new InvalidOperationException("go needs import symbols, use the local override instead");
        }
        public string GetParameterSignature(CodeParameter parameter, CodeElement targetElement)
        {
            return $"{parameter.Name} {GetTypeString(parameter.Type, targetElement)}";
        }
        public string GetTypeString(CodeTypeBase code) => throw new InvalidOperationException("go needs import symbols, use the local override instead");
        public string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool addPointerSymbol = true)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"Go does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var importSymbol = GetImportSymbol(code, targetElement);
                if(!string.IsNullOrEmpty(importSymbol))
                    importSymbol += ".";
                var typeName = TranslateType(currentType.Name);
                var nullableSymbol = addPointerSymbol && 
                                    currentType.IsNullable &&
                                    currentType.CollectionKind == CodeTypeBase.CodeTypeCollectionKind.None &&
                                    !IsScalarType(currentType.Name) ? "*"
                                    : string.Empty;
                var collectionPrefix = currentType.CollectionKind switch {
                    CodeType.CodeTypeCollectionKind.None => string.Empty,
                    _ => "[]",
                };
                if (currentType.ActionOf)
                    return $"func (value {nullableSymbol}{collectionPrefix}{importSymbol}{typeName}) (err error)";
                else
                    return $"{nullableSymbol}{collectionPrefix}{importSymbol}{typeName}";
            }
            else throw new InvalidOperationException($"type of type {code.GetType()} is unknown");
        }

        public string TranslateType(string typeName)
        {
            if(typeName.StartsWith("map[")) return typeName; //casing hack

            return (typeName) switch {
                "void" => string.Empty,
                "string" => $"string",
                "float" => "float32",
                "integer" => "int32",
                "long" => "int64",
                "double" => "float64",
                "boolean" => "bool",
                "guid" => "uuid.UUID",
                "datetimeoffset" => "time.Time",
                "binary" => "[]byte",
                ("String") => TranslateType(typeName.ToFirstCharacterLowerCase()), //casing hack
                _ => typeName.ToFirstCharacterUpperCase() ?? "Object",
            };
        }
        private static bool IsPrimitiveType(string typeName) {
            return typeName switch {
                ("void" or "string" or "float" or "integer" or "long" or "double" or "boolean" or "guid" or "datetimeoffset") => true,
                _ => false,
            };
        }
        public bool IsScalarType(string typeName) {
            if(typeName.StartsWith("map[")) return true;
            return typeName.ToLowerInvariant() switch {
                ("binary" or "void") => true,
                _ => false,
            };
        }
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
                        targetElement is CodeClass targetClass &&
                        targetClass.StartBlock is CodeClass.Declaration targetClassDeclaration) {
                            var symbolUsing = targetClassDeclaration
                                                            .Usings
                                                            .FirstOrDefault(x => currentBaseType.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                            return symbolUsing == null ? string.Empty : symbolUsing.Declaration.Name.GetNamespaceImportSymbol();
                        }
            }
            return string.Empty;
        }

        public void WriteShortDescription(string description, LanguageWriter writer)
        {
            throw new NotImplementedException();
        }

        internal void AddRequestBuilderBody(bool addCurrentPath, string returnType, LanguageWriter writer, string suffix = default)
        {
            var currentPath = addCurrentPath ? $"m.{CurrentPathPropertyName} + " : string.Empty;
            var constructorName = returnType.Split('.').Last().ToFirstCharacterUpperCase();
            writer.WriteLines($"return {returnType}.New{constructorName}({currentPath}m.{PathSegmentPropertyName}{suffix}, m.{HttpCorePropertyName});");
        }
    }
}
