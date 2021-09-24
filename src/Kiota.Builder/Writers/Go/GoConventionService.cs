using System;
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
        private const string HttpCorePropertyName = "httpCore";
        public override string ParseNodeInterfaceName => "ParseNode";
        public object AbstractionsHash => "ida96af0f171bb75f894a4013a6b3146a4397c58f11adb81a2b7cbea9314783a9";
        public override string GetAccessModifier(AccessModifier access)
        {
            throw new InvalidOperationException("go uses a naming convention for access modifiers");
        }
        public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement)
        {
            return $"{parameter.Name} {GetTypeString(parameter.Type, targetElement)}";
        }
        public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = false) =>
            GetTypeString(code, targetElement, includeCollectionInformation: includeCollectionInformation);
        public string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool addPointerSymbol = true, bool includeCollectionInformation = true)
        {
            if(code is CodeUnionType) 
                throw new InvalidOperationException($"Go does not support union types, the union type {code.Name} should have been filtered out by the refiner");
            else if (code is CodeType currentType) {
                var importSymbol = GetImportSymbol(code, targetElement);
                if(!string.IsNullOrEmpty(importSymbol))
                    importSymbol += ".";
                var typeName = TranslateType(currentType);
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

        public override string TranslateType(CodeType type)
        {
            if(type.Name.StartsWith("map[")) return type.Name; //casing hack

            return (type.Name) switch {
                "void" => string.Empty,
                "string" => "string",
                "float" => "float32",
                "integer" => "int32",
                "long" => "int64",
                "double" => "float64",
                "boolean" => "bool",
                "guid" => "uuid.UUID",
                "datetimeoffset" => "time.Time",
                "binary" => "[]byte",
                ("String") => type.Name.ToFirstCharacterLowerCase(), //casing hack
                _ => type.Name.ToFirstCharacterUpperCase() ?? "Object",
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
                ("binary" or "void" or "[]byte") => true,
                _ => false,
            };
        }
        private static string GetImportSymbol(CodeTypeBase currentBaseType, CodeElement targetElement) {
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

        public override void WriteShortDescription(string description, LanguageWriter writer)
        {
            throw new NotImplementedException();
        }

        internal void AddRequestBuilderBody(bool addCurrentPath, string returnType, LanguageWriter writer, string suffix = default)
        {
            var currentPath = addCurrentPath ? $"m.{CurrentPathPropertyName} + " : string.Empty;
            var splatImport = returnType.Split('.');
            var constructorName = splatImport.Last().ToFirstCharacterUpperCase();
            var moduleName = returnType.Length > 1 ? splatImport.First() + "." : string.Empty;
            writer.WriteLines($"return *{moduleName}New{constructorName}({currentPath}m.{PathSegmentPropertyName}{suffix}, m.{HttpCorePropertyName}, false);");
        }
    }
}
