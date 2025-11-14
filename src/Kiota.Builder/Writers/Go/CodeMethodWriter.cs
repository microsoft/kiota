using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Refiners;

namespace Kiota.Builder.Writers.Go;

public class CodeMethodWriter : BaseElementWriter<CodeMethod, GoConventionService>
{
    private readonly bool ExcludeBackwardCompatible;
    public CodeMethodWriter(GoConventionService conventionService, bool excludeBackwardCompatible) : base(conventionService)
    {
        ExcludeBackwardCompatible = excludeBackwardCompatible;
    }
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not IProprietableBlock) throw new InvalidOperationException("the parent of a method should be a class or an interface");

        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement.Parent);
        var writePrototypeOnly = codeElement.Parent is CodeInterface;
        WriteMethodPrototype(codeElement, codeElement.Parent!, writer, returnType, writePrototypeOnly);
        if (writePrototypeOnly || codeElement.Parent is not CodeClass parentClass) return;
        var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
        writer.IncreaseIndent();
        var requestOptionsParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var requestContentType = codeElement.Parameters.OfKind(CodeParameterKind.RequestBodyContentType);
        var requestParams = new RequestParams(requestBodyParam, requestOptionsParam, requestContentType);
        switch (codeElement.Kind)
        {
            case CodeMethodKind.Serializer:
                WriteSerializerBody(parentClass, writer, inherits);
                break;
            case CodeMethodKind.Deserializer:
                WriteDeserializerBody(codeElement, parentClass, writer, inherits);
                break;
            case CodeMethodKind.IndexerBackwardCompatibility:
                WriteIndexerBody(codeElement, parentClass, writer, returnType);
                break;
            case CodeMethodKind.RequestGenerator when codeElement.IsOverload:
                WriteGeneratorMethodCall(codeElement, requestParams, parentClass, writer, "return ");
                break;
            case CodeMethodKind.RequestGenerator when !codeElement.IsOverload:
                WriteRequestGeneratorBody(codeElement, requestParams, writer, parentClass);
                break;
            case CodeMethodKind.RequestExecutor when !codeElement.IsOverload || (codeElement.Deprecation?.IsDeprecated ?? false): //TODO remove deprecation condition for v2
                WriteRequestExecutorBody(codeElement, requestParams, returnType, parentClass, writer);
                break;
            case CodeMethodKind.Getter:
                WriteGetterBody(codeElement, writer, parentClass);
                break;
            case CodeMethodKind.Setter:
                WriteSetterBody(codeElement, writer, parentClass);
                break;
            case CodeMethodKind.RawUrlBuilder:
                WriteRawUrlBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                WriteApiConstructorBody(parentClass, codeElement, writer);
                writer.WriteLine("return m");
                break;
            case CodeMethodKind.Constructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                writer.WriteLine("return m");
                break;
            case CodeMethodKind.RawUrlConstructor:
                WriteRawUrlConstructorBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                WriteRequestBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.RequestBuilderWithParameters:
                WriteRequestBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.Factory:
                WriteFactoryMethodBody(codeElement, parentClass, writer);
                break;
            case CodeMethodKind.ComposedTypeMarker:
                WriteComposedTypeMarkerBody(writer);
                break;
            case CodeMethodKind.ErrorMessageOverride:
                WriteErrorMethodOverride(parentClass, writer);
                break;
            default:
                writer.WriteLine("return nil");
                break;
        }
        writer.CloseBlock();
    }
    private static void WriteErrorMethodOverride(CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.AssociatedInterface is not null && parentClass.IsErrorDefinition && parentClass.AssociatedInterface.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterUpperCase(), static x => x.Name.ToFirstCharacterUpperCase() + "()") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
        {
            writer.WriteLine($"return *(m.{primaryMessageCodePath})");
        }
        else
        {
            writer.WriteLine("return m.ApiError.Error()");
        }
    }
    private void WriteRawUrlBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var rawUrlParameter = codeElement.Parameters.OfKind(CodeParameterKind.RawUrl) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RawUrl parameter");
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RequestAdapter property");
        writer.WriteLine($"return New{parentClass.Name.ToFirstCharacterUpperCase()}({rawUrlParameter.Name.ToFirstCharacterLowerCase()}, m.BaseRequestBuilder.{requestAdapterProperty.Name.ToFirstCharacterUpperCase()});");
    }
    private void WriteComposedTypeMarkerBody(LanguageWriter writer)
    {
        writer.WriteLine("return true");
    }
    private void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode) ?? throw new InvalidOperationException("Factory method should have a ParseNode parameter");
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType || parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            writer.WriteLine($"{ResultVarName} := New{parentClass.Name.ToFirstCharacterUpperCase()}()");
        if (parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck)
            writer.StartBlock($"if {parseNodeParameter.Name.ToFirstCharacterLowerCase()} != nil {{");
        var writeDiscriminatorValueRead = parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck && !parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType;
        if (writeDiscriminatorValueRead)
        {
            writer.WriteLine($"mappingValueNode, err := {parseNodeParameter.Name.ToFirstCharacterLowerCase()}.GetChildNode(\"{parentClass.DiscriminatorInformation.DiscriminatorPropertyName}\")");
            WriteReturnError(writer, codeElement.ReturnType.Name);
            writer.StartBlock("if mappingValueNode != nil {");
            writer.WriteLines($"{DiscriminatorMappingVarName}, err := mappingValueNode.GetStringValue()");
            WriteReturnError(writer, codeElement.ReturnType.Name);
            writer.StartBlock($"if {DiscriminatorMappingVarName} != nil {{");
        }

        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
            WriteFactoryMethodBodyForInheritedModel(parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType && parentClass.DiscriminatorInformation.HasBasicDiscriminatorInformation)
            WriteFactoryMethodBodyForUnionModelForDiscriminatedTypes(codeElement, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteFactoryMethodBodyForIntersectionModel(codeElement, parentClass, parseNodeParameter, writer);

        if (writeDiscriminatorValueRead)
        {
            writer.CloseBlock();
            writer.CloseBlock();
        }

        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType || parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
        {
            if (parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck)
                writer.CloseBlock();
            if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
                WriteFactoryMethodBodyForUnionModelForUnDiscriminatedTypes(parentClass, parseNodeParameter, writer);
            writer.WriteLine($"return {ResultVarName}, nil");
        }
        else
        {
            if (parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck)
                writer.CloseBlock();
            writer.WriteLine($"return New{parentClass.Name.ToFirstCharacterUpperCase()}(), nil");
        }
    }
    private void WriteFactoryMethodBodyForInheritedModel(CodeClass parentClass, LanguageWriter writer)
    {
        writer.StartBlock($"switch *{DiscriminatorMappingVarName} {{");
        foreach (var mappedType in parentClass.DiscriminatorInformation.DiscriminatorMappings)
        {
            writer.WriteLine($"case \"{mappedType.Key}\":");
            writer.IncreaseIndent();
            writer.WriteLine($"return {conventions.GetImportedStaticMethodName(mappedType.Value, parentClass)}(), nil");
            writer.DecreaseIndent();
        }
        writer.CloseBlock();
    }
    private void WriteFactoryMethodBodyForIntersectionModel(CodeMethod codeElement, CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                    .Where(static x => x.Setter != null)
                                    .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                    .OrderBy(static x => x, CodePropertyTypeBackwardComparer)
                                    .ThenBy(static x => x.Name)
                                    .ToArray();
        foreach (var property in otherProps)
        {
            if (property.Type is CodeType propertyType)
            {
                var valueVarName = "val";
                writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if {valueVarName}, err := {parseNodeParameter.Name.ToFirstCharacterLowerCase()}.{GetDeserializationMethodName(propertyType, parentClass)}; {valueVarName} != nil {{");
                var propertyTypeImportName = conventions.GetTypeString(property.Type, parentClass, false, false);
                WriteReturnError(writer, propertyTypeImportName);
                if (propertyType.IsCollection)
                {
                    var isInterfaceType = propertyType.TypeDefinition is CodeInterface;
                    WriteCollectionCast(propertyTypeImportName, valueVarName, "cast", writer, isInterfaceType ? string.Empty : "*", !isInterfaceType);
                    valueVarName = "cast";
                }
                else if (propertyType.TypeDefinition is CodeClass || propertyType.TypeDefinition is CodeInterface || propertyType.TypeDefinition is CodeEnum)
                {
                    if (propertyType.TypeDefinition is CodeEnum)
                    {
                        propertyTypeImportName = conventions.GetTypeString(property.Type, parentClass, false, true);
                    }
                    writer.StartBlock($"if {GetTypeAssertion(valueVarName, propertyTypeImportName, "cast", "ok")}; ok {{");
                    valueVarName = "cast";
                }
                writer.WriteLine($"{ResultVarName}.{property.Setter!.Name.ToFirstCharacterUpperCase()}({valueVarName})");
                if (!propertyType.IsCollection && (propertyType.TypeDefinition is CodeClass || propertyType.TypeDefinition is CodeInterface || propertyType.TypeDefinition is CodeEnum))
                    writer.CloseBlock();
                writer.DecreaseIndent();
            }
            if (!includeElse)
                includeElse = true;
        }
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is CodeType xType && xType.TypeDefinition is CodeClass && !xType.IsCollection)
                                            .Select(static x => new Tuple<CodeProperty, CodeType>(x, (CodeType)x.Type))
                                            .ToArray();
        if (complexProperties.Length != 0)
        {
            if (includeElse)
                writer.StartBlock("} else {");
            foreach (var property in complexProperties.Where(static x => x.Item1.Setter != null))
                writer.WriteLine($"{ResultVarName}.{property.Item1.Setter!.Name.ToFirstCharacterUpperCase()}({conventions.GetImportedStaticMethodName(property.Item2, codeElement)}())");
            if (includeElse)
                writer.CloseBlock();
        }
        else if (otherProps.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
    }
    private const string ResultVarName = "result";
    private const string DiscriminatorMappingVarName = "mappingValue";
    private static readonly CodePropertyTypeComparer CodePropertyTypeForwardComparer = new();
    private static readonly CodePropertyTypeComparer CodePropertyTypeBackwardComparer = new(true);
    private void WriteFactoryMethodBodyForUnionModelForDiscriminatedTypes(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                    .Where(static x => x.Setter != null)
                                    .Where(static x => x.Type is CodeType xType && !xType.IsCollection && (xType.TypeDefinition is CodeClass || xType.TypeDefinition is CodeInterface))
                                    .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                    .ThenBy(static x => x.Name)
                                    .ToArray();
        foreach (var property in otherProps)
        {
            var propertyType = property.Type as CodeType;
            if (propertyType!.TypeDefinition is CodeInterface typeInterface && typeInterface.OriginalClass != null)
                propertyType = new CodeType
                {
                    Name = typeInterface.OriginalClass.Name,
                    TypeDefinition = typeInterface.OriginalClass,
                    CollectionKind = propertyType.CollectionKind,
                    IsNullable = propertyType.IsNullable,
                };
            var mappedType = parentClass.DiscriminatorInformation.DiscriminatorMappings.FirstOrDefault(x => x.Value.Name.Equals(propertyType.Name, StringComparison.OrdinalIgnoreCase));
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if {conventions.StringsHash}.EqualFold(*{DiscriminatorMappingVarName}, \"{mappedType.Key}\") {{");
            writer.WriteLine($"{ResultVarName}.{property.Setter!.Name.ToFirstCharacterUpperCase()}({conventions.GetImportedStaticMethodName(propertyType, codeElement)}())");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherProps.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
    }
    private void WriteFactoryMethodBodyForUnionModelForUnDiscriminatedTypes(CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                    .Where(static x => x.Setter != null)
                                    .Where(static x => x.Type is CodeType xType && (xType.IsCollection || xType.TypeDefinition is null || xType.TypeDefinition is CodeEnum))
                                    .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                    .ThenBy(static x => x.Name)
                                    .ToArray();
        foreach (var property in otherProps)
        {
            var valueVarName = "val";
            var propertyType = property.Type as CodeType;
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if {valueVarName}, err := {parseNodeParameter.Name.ToFirstCharacterLowerCase()}.{GetDeserializationMethodName(propertyType!, parentClass)}; {valueVarName} != nil {{");
            var propertyTypeImportName = conventions.GetTypeString(property.Type, parentClass, false, false);
            WriteReturnError(writer, propertyTypeImportName);
            if (propertyType!.IsCollection)
            {
                var isInterfaceType = propertyType.TypeDefinition is CodeInterface;
                WriteCollectionCast(propertyTypeImportName, valueVarName, "cast", writer, isInterfaceType ? string.Empty : "*", !isInterfaceType);
                valueVarName = "cast";
            }
            writer.WriteLine($"{ResultVarName}.{property.Setter!.Name.ToFirstCharacterUpperCase()}({valueVarName})");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherProps.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
    }

    private void WriteMethodDocumentation(CodeMethod code, string methodName, LanguageWriter writer)
    {
        conventions.WriteShortDescription(code, writer, $"{methodName.ToFirstCharacterUpperCase()} ");
        conventions.WriteDeprecation(code, writer);
        if (!"void".Equals(code.ReturnType.Name, StringComparison.OrdinalIgnoreCase))
        {
            var shortReturnTypeName = conventions.GetTypeString(code.ReturnType, code, true, true, false);
            conventions.WriteDescriptionItem($"returns a {shortReturnTypeName} when successful", writer);
        }
        WriteThrownExceptions(code, writer);
        conventions.WriteLinkDescription(code.Documentation, writer);
    }
    private void WriteThrownExceptions(CodeMethod code, LanguageWriter writer)
    {
        if (code.Kind is not CodeMethodKind.RequestExecutor) return;
        foreach (var errorMapping in code.ErrorMappings)
        {
            var statusCode = errorMapping.Key.ToUpperInvariant() switch
            {
                "XXX" => "4XX or 5XX",
                _ => errorMapping.Key,
            };
            var errorTypeString = conventions.GetTypeString(errorMapping.Value, code, false, false, false);
            conventions.WriteDescriptionItem($"returns a {errorTypeString} error when the service returns a {statusCode} status code", writer);
        }
    }
    private const string TempParamsVarName = "urlParams";
    private static void WriteRawUrlConstructorBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement.OriginalMethod == null ||
            codeElement.Parameters.OfKind(CodeParameterKind.RequestAdapter) is not CodeParameter requestAdapterParam ||
            codeElement.Parameters.OfKind(CodeParameterKind.RawUrl) is not CodeParameter rawUrlParam)
            return;
        var pathParamsSuffix = string.Join(", ", codeElement.OriginalMethod.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)).Select(static x => "nil").ToArray());
        if (!string.IsNullOrEmpty(pathParamsSuffix)) pathParamsSuffix = ", " + pathParamsSuffix;
        writer.WriteLines($"{TempParamsVarName} := make(map[string]string)",
                        $"{TempParamsVarName}[\"request-raw-url\"] = {rawUrlParam.Name.ToFirstCharacterLowerCase()}",
                        $"return New{parentClass.Name.ToFirstCharacterUpperCase()}Internal({TempParamsVarName}, {requestAdapterParam.Name.ToFirstCharacterLowerCase()}{pathParamsSuffix})");
    }
    private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
        conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, pathParameters: codeElement.Parameters.Where(static x => x.IsOfKind(CodeParameterKind.Path)));
    }
    private void WriteSerializerBody(CodeClass parentClass, LanguageWriter writer, bool inherits)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteSerializerBodyForUnionModel(parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteSerializerBodyForIntersectionModel(parentClass, writer);
        else
            WriteSerializerBodyForInheritedModel(inherits, parentClass, writer);

        if (parentClass.GetPropertyOfKindFromAccessorOrDirect(CodePropertyKind.AdditionalData) is CodeProperty additionalDataProperty)
        {
            var shouldDeclareErrorVar = !inherits;
            writer.StartBlock();
            writer.WriteLine($"err {errorVarDeclaration(shouldDeclareErrorVar)}= writer.WriteAdditionalData(m.Get{additionalDataProperty.Name.ToFirstCharacterUpperCase()}())");
            WriteReturnError(writer);
            writer.CloseBlock();
        }
        writer.WriteLine("return nil");
    }
    private void WriteSerializerBodyForInheritedModel(bool inherits, CodeClass parentClass, LanguageWriter writer)
    {
        if (inherits)
        {
            writer.WriteLine($"err := m.{parentClass.StartBlock.Inherits!.Name.ToFirstCharacterUpperCase()}.Serialize(writer)");
            WriteReturnError(writer);
        }
        foreach (var otherProp in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom).Where(static x => !x.ExistsInBaseType && !x.ReadOnly && x.Getter != null))
        {
            WriteSerializationMethodCall(otherProp.Type, parentClass, otherProp.WireName, $"m.{otherProp.Getter!.Name.ToFirstCharacterUpperCase()}()", !inherits, writer);
        }
    }
    private void WriteSerializerBodyForUnionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass
                                .GetPropertiesOfKind(CodePropertyKind.Custom)
                                .Where(static x => !x.ExistsInBaseType && x.Getter != null)
                                .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                .ThenBy(static x => x.Name)
                                .ToArray();
        foreach (var otherProp in otherProps)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if m.{otherProp.Getter!.Name.ToFirstCharacterUpperCase()}() != nil {{");
            WriteSerializationMethodCall(otherProp.Type, parentClass, string.Empty, $"m.{otherProp.Getter.Name.ToFirstCharacterUpperCase()}()", true, writer, false);
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherProps.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
    }
    private void WriteSerializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = parentClass
                                .GetPropertiesOfKind(CodePropertyKind.Custom)
                                .Where(static x => !x.ExistsInBaseType)
                                .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                .OrderBy(static x => x, CodePropertyTypeBackwardComparer)
                                .ThenBy(static x => x.Name)
                                .ToArray();
        foreach (var otherProp in otherProps.Where(static x => x.Getter != null))
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if m.{otherProp.Getter!.Name.ToFirstCharacterUpperCase()}() != nil {{");
            WriteSerializationMethodCall(otherProp.Type, parentClass, string.Empty, $"m.{otherProp.Getter.Name.ToFirstCharacterUpperCase()}()", true, writer, false);
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && !x.Type.IsCollection)
                                            .ToArray();
        if (complexProperties.Length != 0)
        {
            if (includeElse)
            {
                writer.WriteLine("} else {");
                writer.IncreaseIndent();
            }
            var propertiesNames = complexProperties
                                .Where(static x => x.Getter != null)
                                .Select(static x => $"m.{x.Getter!.Name.ToFirstCharacterUpperCase()}()")
                                .OrderBy(static x => x)
                                .Aggregate(static (x, y) => $"{x}, {y}");
            WriteSerializationMethodCall(complexProperties.First().Type, parentClass, string.Empty, propertiesNames, true, writer);
            if (includeElse)
            {
                writer.CloseBlock();
            }
        }
        else if (otherProps.Length != 0)
        {
            writer.CloseBlock(decreaseIndent: false);
        }
    }
    private static string errorVarDeclaration(bool shouldDeclareErrorVar) => shouldDeclareErrorVar ? ":" : string.Empty;
    private static readonly GoCodeParameterOrderComparer ParameterOrderComparer = new();

    private void WriteMethodPrototype(CodeMethod code, CodeElement parentBlock, LanguageWriter writer, string returnType, bool writePrototypeOnly)
    {
        var returnTypeAsyncSuffix = code.IsAsync ? "error" : string.Empty;
        if (!string.IsNullOrEmpty(returnType) && code.IsAsync)
            returnTypeAsyncSuffix = $", {returnTypeAsyncSuffix}";
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
        var methodName = code.Kind switch
        {
            CodeMethodKind.Constructor when parentBlock is CodeClass parentClass && parentClass.IsOfKind(CodeClassKind.RequestBuilder) => $"New{parentClass.Name.ToFirstCharacterUpperCase()}Internal", // internal instantiation with url template parameters
            CodeMethodKind.Factory => $"Create{parentBlock.Name.ToFirstCharacterUpperCase()}FromDiscriminatorValue",
            _ when isConstructor => $"New{parentBlock.Name.ToFirstCharacterUpperCase()}",
            _ when code.Access == AccessModifier.Public => code.Name.ToFirstCharacterUpperCase(),
            _ => code.Name.ToFirstCharacterLowerCase()
        };
        if (!writePrototypeOnly)
            WriteMethodDocumentation(code, methodName, writer);
        var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, ParameterOrderComparer).Select(p => conventions.GetParameterSignature(p, parentBlock)).ToList());
        var classType = conventions.GetTypeString(new CodeType { Name = parentBlock.Name, TypeDefinition = parentBlock }, parentBlock);
        var associatedTypePrefix = isConstructor || code.IsStatic || writePrototypeOnly ? string.Empty : $"(m {classType}) ";
        var finalReturnType = isConstructor ? classType : $"{returnType}{returnTypeAsyncSuffix}";
        var errorDeclaration = code.IsOfKind(CodeMethodKind.ClientConstructor,
                                            CodeMethodKind.Constructor,
                                            CodeMethodKind.Getter,
                                            CodeMethodKind.Setter,
                                            CodeMethodKind.IndexerBackwardCompatibility,
                                            CodeMethodKind.Deserializer,
                                            CodeMethodKind.RequestBuilderWithParameters,
                                            CodeMethodKind.RequestBuilderBackwardCompatibility,
                                            CodeMethodKind.RawUrlConstructor,
                                            CodeMethodKind.ComposedTypeMarker,
                                            CodeMethodKind.RawUrlBuilder,
                                            CodeMethodKind.ErrorMessageOverride) || code.IsAsync ?
                                                string.Empty :
                                                "error";
        if (!string.IsNullOrEmpty(finalReturnType) && !string.IsNullOrEmpty(errorDeclaration))
            finalReturnType += ", ";
        var openingBracket = writePrototypeOnly ? string.Empty : " {";
        var funcPrefix = writePrototypeOnly ? string.Empty : "func ";
        writer.WriteLine($"{funcPrefix}{associatedTypePrefix}{methodName}({parameters})({finalReturnType}{errorDeclaration}){openingBracket}");
    }
    private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass)
    {
        var backingStore = parentClass.GetBackingStoreProperty();
        if (backingStore == null || (codeElement.AccessedProperty?.IsOfKind(CodePropertyKind.BackingStore) ?? false))
            writer.WriteLine($"return m.{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}");
        else
            if (codeElement.AccessedProperty != null &&
                !(codeElement.AccessedProperty.Type?.IsNullable ?? true) &&
                !codeElement.AccessedProperty.ReadOnly &&
                !string.IsNullOrEmpty(codeElement.AccessedProperty.DefaultValue))
        {
            writer.WriteLines(
                $"val , err :=  m.{backingStore.NamePrefix}{backingStore.Name.ToFirstCharacterLowerCase()}.Get(\"{codeElement.AccessedProperty.Name.ToFirstCharacterLowerCase()}\")");
            writer.WriteBlock("if err != nil {", "}", "panic(err)");
            writer.WriteBlock("if val == nil {", "}",
                $"var value = {codeElement.AccessedProperty.DefaultValue};",
                $"m.Set{codeElement.AccessedProperty.Name?.ToFirstCharacterUpperCase()}(value);");

            writer.WriteLine($"return val.({conventions.GetTypeString(codeElement.AccessedProperty.Type, parentClass)})");
        }
        else
        {
            var returnType = conventions.GetTypeString(codeElement.ReturnType, parentClass);

            writer.WriteLine($"val, err := m.Get{backingStore.Name.ToFirstCharacterUpperCase()}().Get(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\")");

            writer.WriteBlock("if err != nil {", "}", "panic(err)");
            writer.WriteBlock("if val != nil {", "}", $"return val.({returnType})");

            writer.WriteLine("return nil");
        }
    }
    private void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is not CodeProperty requestAdapterProperty) return;
        var requestAdapterPropertyName = BaseRequestBuilderVarName + "." + requestAdapterProperty.Name.ToFirstCharacterUpperCase();
        var backingStoreParameter = method.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.BackingStore));
        WriteSerializationRegistration(method.SerializerModules, writer, parentClass, "RegisterDefaultSerializer", "SerializationWriterFactory");
        WriteSerializationRegistration(method.DeserializerModules, writer, parentClass, "RegisterDefaultDeserializer", "ParseNodeFactory");
        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock($"if m.{requestAdapterPropertyName}.GetBaseUrl() == \"\" {{");
            writer.WriteLine($"m.{requestAdapterPropertyName}.SetBaseUrl(\"{method.BaseUrl}\")");
            writer.CloseBlock();
            if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty)
                writer.WriteLine($"m.{BaseRequestBuilderVarName}.{pathParametersProperty.Name.ToFirstCharacterUpperCase()}[\"baseurl\"] = m.{requestAdapterPropertyName}.GetBaseUrl()");
        }
        if (backingStoreParameter != null)
            writer.WriteLine($"m.{requestAdapterPropertyName}.EnableBackingStore({backingStoreParameter.Name});");
    }
    private void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, CodeClass parentClass, string methodName, string interfaceName)
    {
        var interfaceImportSymbol = conventions.GetTypeString(new CodeType { Name = interfaceName, IsExternal = true }, parentClass, false, false);
        var methodImportSymbol = conventions.GetTypeString(new CodeType { Name = methodName, IsExternal = true }, parentClass, false, false);
        if (serializationModules != null)
            foreach (var module in serializationModules)
            {
                var moduleImportSymbol = conventions.GetTypeString(new CodeType { Name = module, IsExternal = true }, parentClass, false, false);
                moduleImportSymbol = moduleImportSymbol.Split('.').First();
                writer.WriteLine($"{methodImportSymbol}(func() {interfaceImportSymbol} {{ return {moduleImportSymbol}.New{module}() }})");
            }
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits)
    {
        writer.WriteLine($"m := &{parentClass.Name.ToFirstCharacterUpperCase()}{{");
        if (inherits || parentClass.IsErrorDefinition)
        {
            writer.IncreaseIndent();
            var parentClassName = parentClass.StartBlock.Inherits!.Name.ToFirstCharacterUpperCase();
            var newMethodName = conventions.GetImportedStaticMethodName(parentClass.StartBlock.Inherits, parentClass);
            if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
                currentMethod.Parameters.OfKind(CodeParameterKind.RequestAdapter) is CodeParameter requestAdapterParameter &&
                parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty)
            {
                var pathParametersValue = ", map[string]string{}";
                if (currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParameter)
                    pathParametersValue = $", {pathParametersParameter.Name.ToFirstCharacterLowerCase()}";
                writer.WriteLine($"{parentClassName}: *{newMethodName}({requestAdapterParameter.Name.ToFirstCharacterLowerCase()}, {urlTemplateProperty.DefaultValue}{pathParametersValue}),");
            }
            else
                writer.WriteLine($"{parentClassName}: *{newMethodName}(),");
            writer.DecreaseIndent();
        }
        writer.CloseBlock(decreaseIndent: false);
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                        CodePropertyKind.RequestBuilder)
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue))
                                        .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"m.{propWithDefault.NamePrefix}{propWithDefault.Name.ToFirstCharacterLowerCase()} = {propWithDefault.DefaultValue};");
        }
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData, CodePropertyKind.Custom) //additional data and custom rely on accessors
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue))
                                        // do not apply the default value if the type is composed as the default value may not necessarily which type to use
                                        .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
                                        .OrderBy(static x => x.Name))
        {
            var defaultValueReference = propWithDefault.DefaultValue;
            if (propWithDefault.Type.IsNullable &&
                    defaultValueReference.TrimQuotes().Equals("null", StringComparison.OrdinalIgnoreCase))
            {// avoid setting null as a string.
                defaultValueReference = "nil";
            }
            else if (defaultValueReference.StartsWith('"'))
            {
                defaultValueReference = $"{propWithDefault.Name.ToFirstCharacterLowerCase()}Value";
                var defaultValue = propWithDefault.DefaultValue;
                if (propWithDefault.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum enumDefinition)
                {
                    defaultValue = defaultValue.Trim('"');
                    defaultValue =
                        enumDefinition.Options.FirstOrDefault(x => x.SerializationName.Equals(defaultValue, StringComparison.OrdinalIgnoreCase))?.Name ?? defaultValue;
                    defaultValue = $"{defaultValue.ToUpperInvariant()}_{enumDefinition.Name.ToUpperInvariant()}";
                }
                else if (propWithDefault.Type is CodeType propType && propType.Name.Equals("boolean", StringComparison.OrdinalIgnoreCase))
                {
                    defaultValue = defaultValue.TrimQuotes();
                }
                writer.WriteLine($"{defaultValueReference} := {defaultValue}");
                defaultValueReference = $"&{defaultValueReference}";
            }
            var setterName = propWithDefault.SetterFromCurrentOrBaseType?.Name.ToFirstCharacterUpperCase() is string sName && !string.IsNullOrEmpty(sName) ? sName : $"Set{propWithDefault.Name.ToFirstCharacterUpperCase()}";
            writer.WriteLine($"m.{setterName}({defaultValueReference})");
        }
        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) && currentMethod.IsOfKind(CodeMethodKind.Constructor) &&
            currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParam &&
            parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty)
            conventions.AddParametersAssignment(writer,
                                                    pathParametersParam.Type.AllTypes.FirstOrDefault(),
                                                    pathParametersParam.Name.ToFirstCharacterLowerCase(),
                                                    $"m.{BaseRequestBuilderVarName}.{pathParametersProperty.Name.ToFirstCharacterUpperCase()}",
                                                    false,
                                                    currentMethod.Parameters
                                                                .Where(static x => x.IsOfKind(CodeParameterKind.Path))
                                                                .Select(x => (x.Type, string.IsNullOrEmpty(x.SerializationName) ? x.Name : x.SerializationName, x.Name.ToFirstCharacterLowerCase()))
                                                                .ToArray());
    }
    private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass)
    {
        var backingStore = parentClass.GetBackingStoreProperty();
        if (backingStore == null || (codeElement.AccessedProperty?.IsOfKind(CodePropertyKind.BackingStore) ?? false))
            writer.WriteLine($"m.{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()} = value");
        else
        {
            writer.WriteLine($"err := m.Get{backingStore.Name.ToFirstCharacterUpperCase()}().Set(\"{codeElement.AccessedProperty?.Name?.ToFirstCharacterLowerCase()}\", value)");
            writer.WriteBlock("if err != nil {", "}", "panic(err)");
        }
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, string returnType)
    {
        var pathParameters = codeElement.Parameters.Where(static x => x.IsOfKind(CodeMethod.ParameterKindForConvertedIndexers)).ToArray();
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty)
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"m.BaseRequestBuilder.{pathParametersProperty.Name.ToFirstCharacterUpperCase()}", string.Empty, true, pathParameters.Select(static x => (x.Type, x.SerializationName, x.Name.ToFirstCharacterLowerCase())).ToArray());
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName, codeElement.Parameters.Except(pathParameters).ToArray());
    }
    private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, bool inherits)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteDeserializerBodyForUnionModel(codeElement, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteDeserializerBodyForIntersectionModel(codeElement, parentClass, writer);
        else
            WriteDeserializerBodyForInheritedModel(codeElement, parentClass, writer, inherits);
    }
    private static void WriteDeserializerBodyForUnionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherPropGetters = parentClass
                                .GetPropertiesOfKind(CodePropertyKind.Custom)
                                .Where(static x => !x.ExistsInBaseType && x.Getter != null)
                                .Where(static x => x.Type is CodeType propertyType && !propertyType.IsCollection && propertyType.TypeDefinition is CodeInterface)
                                .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                .ThenBy(static x => x.Name)
                                .Select(static x => x.Getter!.Name.ToFirstCharacterUpperCase())
                                .ToArray();
        foreach (var otherPropGetter in otherPropGetters)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if m.{otherPropGetter}() != nil {{");
            writer.WriteLine($"return m.{otherPropGetter}().{method.Name.ToFirstCharacterUpperCase()}()");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherPropGetters.Length != 0)
            writer.CloseBlock(decreaseIndent: false);
        writer.WriteLine($"return make({method.ReturnType.Name})");
    }
    private void WriteDeserializerBodyForIntersectionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && !x.Type.IsCollection)
                                            .ToArray();
        if (complexProperties.Length != 0)
        {
            var propertiesNames = complexProperties
                                .Where(static x => x.Getter != null)
                                .Select(static x => x.Getter!.Name.ToFirstCharacterUpperCase())
                                .OrderBy(static x => x)
                                .ToArray();
            var propertiesNamesAsConditions = propertiesNames
                                .Select(static x => $"m.{x}() != nil")
                                .Aggregate(static (x, y) => $"{x} || {y}");
            writer.StartBlock($"if {propertiesNamesAsConditions} {{");
            var propertiesNamesAsArgument = propertiesNames
                                .Aggregate(static (x, y) => $"m.{x}(), m.{y}()");
            writer.WriteLine($"return {conventions.SerializationHash}.MergeDeserializersForIntersectionWrapper({propertiesNamesAsArgument})");
            writer.CloseBlock();
        }
        writer.WriteLine($"return make({method.ReturnType.Name})");
    }
    private void WriteDeserializerBodyForInheritedModel(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, bool inherits)
    {
        var fieldToSerialize = parentClass
                                .GetPropertiesOfKind(CodePropertyKind.Custom)
                                .Where(static x => !x.ExistsInBaseType)
                                .OrderBy(static x => x.Name)
                                .Where(static x => x.Setter != null)
                                .ToArray();
        if (inherits)
            writer.WriteLine($"res := m.{parentClass.StartBlock.Inherits!.Name.ToFirstCharacterUpperCase()}.{codeElement.Name.ToFirstCharacterUpperCase()}()");
        else
            writer.WriteLine($"res := make({codeElement.ReturnType.Name})");
        if (fieldToSerialize.Length != 0)
        {
            var parsableImportSymbol = GetConversionHelperMethodImport(parentClass, "ParseNode");
            foreach (var field in fieldToSerialize)
                WriteFieldDeserializer(field, writer, parentClass, parsableImportSymbol);
        }
        writer.WriteLine("return res");
    }

    private void WriteFieldDeserializer(CodeProperty property, LanguageWriter writer, CodeClass parentClass, string parsableImportSymbol)
    {
        if (property.Setter is null) return;
        writer.StartBlock($"res[\"{property.WireName}\"] = func (n {parsableImportSymbol}) error {{");
        var propertyTypeImportName = conventions.GetTypeString(property.Type, parentClass, false, false);
        var deserializationMethodName = GetDeserializationMethodName(property.Type, parentClass);
        writer.WriteLine($"val, err := n.{deserializationMethodName}");
        WriteReturnError(writer);
        writer.StartBlock("if val != nil {");
        var (valueArgument, pointerSymbol, dereference) = (property.Type.AllTypes.First().TypeDefinition, property.Type.IsCollection) switch
        {
            (CodeClass, false) or (CodeEnum, false) => (GetTypeAssertion("val", $"*{propertyTypeImportName}"), string.Empty, true),
            (CodeClass, true) or (CodeEnum, true) => ("res", "*", true),
            (CodeInterface, false) => (GetTypeAssertion("val", propertyTypeImportName), string.Empty, false),
            (CodeInterface, true) => ("res", string.Empty, false),
            (_, true) => ("res", "*", true),
            (null, false) when property.Type.AllTypes.First().Name.Equals(GoRefiner.UntypedNodeName, StringComparison.OrdinalIgnoreCase) => (GetTypeAssertion("val", $"{propertyTypeImportName}"), string.Empty, true),
            _ => ("val", string.Empty, true),
        };
        if (property.Type.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None)
            WriteCollectionCast(propertyTypeImportName, "val", "res", writer, pointerSymbol, dereference);
        writer.WriteLine($"m.{property.Setter.Name.ToFirstCharacterUpperCase()}({valueArgument})");
        writer.CloseBlock();
        writer.WriteLine("return nil");
        writer.CloseBlock();
    }

    private static string GetTypeAssertion(string originalReference, string typeImportName, string? assignVarName = default, string? statusVarName = default) =>
        $"{assignVarName}{(!string.IsNullOrEmpty(statusVarName) && !string.IsNullOrEmpty(assignVarName) ? ", " : string.Empty)}{statusVarName}{(string.IsNullOrEmpty(statusVarName) && string.IsNullOrEmpty(assignVarName) ? string.Empty : " := ")}{originalReference}.({typeImportName})";

    private static void WriteCollectionCast(string propertyTypeImportName, string sourceVarName, string targetVarName, LanguageWriter writer, string pointerSymbol = "*", bool dereference = true, bool scalar = false)
    {
        writer.WriteLines($"{targetVarName} := make([]{propertyTypeImportName}, len({sourceVarName}))",
            $"for i, v := range {sourceVarName} {{");
        writer.IncreaseIndent();
        if (!scalar)
        {
            writer.StartBlock("if v != nil {");
            var derefPrefix = dereference ? "*(" : string.Empty;
            var derefSuffix = dereference ? ")" : string.Empty;
            writer.WriteLine($"{targetVarName}[i] = {GetTypeAssertion(derefPrefix + "v", pointerSymbol + propertyTypeImportName)}{derefSuffix}");
            writer.CloseBlock();
        }
        else
        {
            writer.WriteLine($"{targetVarName}[i] = v");
        }
        writer.CloseBlock();
    }

    private static string getSendMethodName(string returnType, CodeMethod codeElement, bool isPrimitive, bool isBinary, bool isEnum)
    {
        return returnType switch
        {
            "void" => "SendNoContent",
            _ when string.IsNullOrEmpty(returnType) => "SendNoContent",
            _ when codeElement.ReturnType.IsCollection && isPrimitive => "SendPrimitiveCollection",
            _ when isPrimitive || isBinary => "SendPrimitive",
            _ when codeElement.ReturnType.IsCollection && !isEnum => "SendCollection",
            _ when codeElement.ReturnType.IsCollection && isEnum => "SendEnumCollection",
            _ when isEnum => "SendEnum",
            _ => "Send"
        };
    }
    private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, string returnType, CodeClass parentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        if (returnType == null) throw new InvalidOperationException("return type cannot be null"); // string.Empty is a valid return type
        var isPrimitive = conventions.IsPrimitiveType(returnType);
        var isBinary = conventions.StreamTypeName.Equals(returnType.TrimStart('*'), StringComparison.OrdinalIgnoreCase);
        var isEnum = codeElement.ReturnType is CodeType collType && collType.TypeDefinition is CodeEnum;
        var sendMethodName = getSendMethodName(returnType, codeElement, isPrimitive, isBinary, isEnum);
        var contextVarName = codeElement.Parameters.OfKind(CodeParameterKind.Cancellation)?.Name;
        var typeShortName = returnType.Split('.').Last().ToFirstCharacterUpperCase();
        var isVoid = string.IsNullOrEmpty(typeShortName);
        WriteGeneratorMethodCall(codeElement, requestParams, parentClass, writer, $"{RequestInfoVarName}, err := ");
        WriteReturnError(writer, returnType);
        var constructorFunction = returnType switch
        {
            _ when isVoid => string.Empty,
            _ when isPrimitive => $"\"{returnType.TrimCollectionAndPointerSymbols().TrimPackageReference().ToLowerInvariant()}\", ",
            _ when isBinary => $"\"{returnType}\", ",
            _ when isEnum => $"{conventions.GetImportedStaticMethodName(codeElement.ReturnType, parentClass, "Parse", string.Empty, string.Empty)}, ",
            _ => $"{conventions.GetImportedStaticMethodName(codeElement.ReturnType, parentClass, "Create", "FromDiscriminatorValue", "able")}, ",
        };
        var errorMappingVarName = "nil";
        if (codeElement.ErrorMappings.Any())
        {
            errorMappingVarName = "errorMapping";
            writer.WriteLine($"{errorMappingVarName} := {conventions.AbstractionsHash}.ErrorMappings {{");
            writer.IncreaseIndent();
            foreach (var errorMapping in codeElement.ErrorMappings)
            {
                writer.WriteLine($"\"{errorMapping.Key.ToUpperInvariant()}\": {conventions.GetImportedStaticMethodName(errorMapping.Value, parentClass, "Create", "FromDiscriminatorValue", "able")},");
            }
            writer.CloseBlock();
        }

        var assignmentPrefix = isVoid ?
                    "err =" :
                    "res, err :=";
        writer.WriteLine($"{assignmentPrefix} m.{BaseRequestBuilderVarName}.RequestAdapter.{sendMethodName}({contextVarName}, {RequestInfoVarName}, {constructorFunction}{errorMappingVarName})");
        WriteReturnError(writer, returnType);
        var valueVarName = string.Empty;
        if (codeElement.ReturnType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None)
        {
            var propertyTypeImportName = conventions.GetTypeString(codeElement.ReturnType, parentClass, false, false);
            var isInterface = codeElement.ReturnType.AllTypes.First().TypeDefinition is CodeInterface;
            WriteCollectionCast(propertyTypeImportName, "res", "val", writer, isInterface || isEnum ? string.Empty : "*", !(isInterface || isEnum));
            valueVarName = "val, ";
        }
        else if (!isVoid)
        {
            writer.WriteLine("if res == nil {");
            writer.IncreaseIndent();
            writer.WriteLine("return nil, nil");
            writer.CloseBlock();
        }
        var resultReturnCast = isVoid switch
        {
            true => string.Empty,
            _ when !string.IsNullOrEmpty(valueVarName) => valueVarName,
            _ => $"{GetTypeAssertion("res", returnType)}, "
        };
        writer.WriteLine($"return {resultReturnCast}nil");
    }
    private static void WriteMethodCall(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, LanguageWriter writer, CodeMethodKind kind, Func<string?, string, string> template, int parametersPad = 0)
    {
        var generatorMethodName = parentClass
                                            .Methods
                                            .OrderBy(x => x.IsOverload)
                                            .FirstOrDefault(x => x.IsOfKind(kind) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name
                                            ?.ToFirstCharacterUpperCase();
        var paramsList = new List<CodeParameter?> { codeElement.Parameters.OfKind(CodeParameterKind.Cancellation), requestParams.requestBody, requestParams.requestConfiguration };
        if (requestParams.requestContentType is not null)
            paramsList.Insert(2, requestParams.requestContentType);
        if (parametersPad > 0)
            paramsList.AddRange(Enumerable.Range(0, parametersPad).Select<int, CodeParameter?>(static x => null));
        var requestInfoParameters = paramsList.OfType<CodeParameter>()
                                            .Select(static x => x.Name)
                                            .ToList();
        var skipIndex = requestParams.requestBody == null ? 1 : 0;
        requestInfoParameters.AddRange(paramsList.Where(static x => x == null).Skip(skipIndex).Select(static x => "nil"));

        var paramsCall = string.Join(", ", requestInfoParameters);

        writer.WriteLine(template(generatorMethodName, paramsCall));
    }

    private static void WriteGeneratorMethodCall(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, LanguageWriter writer, string prefix)
    {
        WriteMethodCall(codeElement, requestParams, parentClass, writer, CodeMethodKind.RequestGenerator, (name, paramsCall) =>
            $"{prefix}m.{name}({paramsCall});"
        );
    }
    private const string RequestInfoVarName = "requestInfo";
    private static void WriteLegacyRequestConfiguration(RequestParams requestParams, LanguageWriter writer)
    {//TODO remove for v2
        if (requestParams.requestConfiguration is null) return;
        writer.StartBlock($"if {requestParams.requestConfiguration.Name} != nil {{");

        if (requestParams.QueryParameters is { } queryString)
        {
            var queryStringName = $"{requestParams.requestConfiguration.Name}.{queryString.Name.ToFirstCharacterUpperCase()}";
            writer.StartBlock($"if {queryStringName} != nil {{");
            writer.WriteLine($"requestInfo.AddQueryParameters(*({queryStringName}))");
            writer.CloseBlock();
        }
        if (requestParams.Headers is { } headers)
        {
            var headersName = $"{requestParams.requestConfiguration.Name}.{headers.Name.ToFirstCharacterUpperCase()}";
            writer.WriteLine($"{RequestInfoVarName}.Headers.AddAll({headersName})");
        }
        if (requestParams.Options is { } options)
        {
            var optionsName = $"{requestParams.requestConfiguration.Name}.{options.Name.ToFirstCharacterUpperCase()}";
            writer.WriteLine($"{RequestInfoVarName}.AddRequestOptions({optionsName})");
        }
        writer.CloseBlock();
    }
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, LanguageWriter writer, CodeClass parentClass)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        if (parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is not CodeProperty urlTemplateProperty) throw new InvalidOperationException("url template property cannot be null");
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is not CodeProperty urlTemplateParamsProperty) throw new InvalidOperationException("url template params property cannot be null");

        var requestAdapterPropertyName = BaseRequestBuilderVarName + "." + parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter)?.Name.ToFirstCharacterUpperCase();
        var contextParameterName = codeElement.Parameters.OfKind(CodeParameterKind.Cancellation)?.Name.ToFirstCharacterLowerCase();
        var urlTemplateValue = codeElement.HasUrlTemplateOverride ? $"\"{codeElement.UrlTemplateOverride}\"" : GetPropertyCall(urlTemplateProperty, "\"\"");
        writer.WriteLine($"{RequestInfoVarName} := {conventions.AbstractionsHash}.NewRequestInformationWithMethodAndUrlTemplateAndPathParameters({conventions.AbstractionsHash}.{codeElement.HttpMethod.Value.ToString().ToUpperInvariant()}, {urlTemplateValue}, {GetPropertyCall(urlTemplateParamsProperty, "\"\"")})");

        if (ExcludeBackwardCompatible && requestParams.requestConfiguration is not null)
            writer.WriteLine($"{conventions.AbstractionsHash}.ConfigureRequestInformation({RequestInfoVarName}, {requestParams.requestConfiguration.Name})");
        else
            WriteLegacyRequestConfiguration(requestParams, writer);

        if (codeElement.ShouldAddAcceptHeader)
            writer.WriteLine($"{RequestInfoVarName}.Headers.TryAdd(\"Accept\", \"{codeElement.AcceptHeaderValue.SanitizeDoubleQuote()}\")");
        if (requestParams.requestBody != null)
        {
            var bodyParamReference = $"{requestParams.requestBody.Name.ToFirstCharacterLowerCase()}";
            var collectionSuffix = requestParams.requestBody.Type.IsCollection ? "Collection" : string.Empty;
            var sanitizedRequestBodyContentType = codeElement.RequestBodyContentType.SanitizeDoubleQuote();
            if (requestParams.requestBody.Type.Name.Equals("binary", StringComparison.OrdinalIgnoreCase) || requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (requestParams.requestContentType is not null)
                    writer.WriteLine($"{RequestInfoVarName}.SetStreamContentAndContentType({bodyParamReference}, *{requestParams.requestContentType.Name.ToFirstCharacterLowerCase()})");
                else if (!string.IsNullOrEmpty(sanitizedRequestBodyContentType))
                    writer.WriteLine($"{RequestInfoVarName}.SetStreamContentAndContentType({bodyParamReference}, \"{sanitizedRequestBodyContentType}\")");
            }
            else if (requestParams.requestBody.Type is CodeType bodyType && (bodyType.TypeDefinition is CodeClass || bodyType.TypeDefinition is CodeInterface || bodyType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)))
            {
                if (bodyType.IsCollection)
                {
                    var parsableSymbol = GetConversionHelperMethodImport(parentClass, "Parsable");
                    WriteCollectionCast(parsableSymbol, bodyParamReference, "cast", writer, string.Empty, false);
                    bodyParamReference = "cast";
                }
                writer.WriteLine($"err := {RequestInfoVarName}.SetContentFromParsable{collectionSuffix}({contextParameterName}, m.{requestAdapterPropertyName}, \"{sanitizedRequestBodyContentType}\", {bodyParamReference})");
                writer.WriteBlock("if err != nil {", "}", "return nil, err");
            }
            else
            {
                if (requestParams.requestBody.Type.IsCollection)
                {
                    WriteCollectionCast("interface{}", bodyParamReference, "cast", writer, string.Empty, false, true);
                    bodyParamReference = "cast";
                }
                writer.WriteLine($"{RequestInfoVarName}.SetContentFromScalar{collectionSuffix}({contextParameterName}, m.{requestAdapterPropertyName}, \"{sanitizedRequestBodyContentType}\", {bodyParamReference})");
            }
        }

        writer.WriteLine($"return {RequestInfoVarName}, nil");
    }
    private const string BaseRequestBuilderVarName = "BaseRequestBuilder";
    private static string GetPropertyCall(CodeProperty property, string defaultValue)
    {
        return property switch
        {
            null => defaultValue,
            _ when property.ExistsInExternalBaseType => $"m.{BaseRequestBuilderVarName}.{property.Name.ToFirstCharacterUpperCase()}",
            _ => $"m.{property.Name.ToFirstCharacterLowerCase()}"
        };
    }

    private static void WriteReturnError(LanguageWriter writer, params string[] returnTypes)
    {
        writer.WriteLine("if err != nil {");
        writer.IncreaseIndent();
        var nilsPrefix = GetNilsErrorPrefix(returnTypes);
        writer.WriteLine($"return {nilsPrefix}err");
        writer.CloseBlock();
    }
    private static string GetNilsErrorPrefix(params string[] returnTypes)
    {
        var sanitizedTypes = returnTypes.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        return sanitizedTypes.Length == 0 ?
                        string.Empty :
                        sanitizedTypes.Select(static _ => "nil").Aggregate((x, y) => $"{x}, {y}") + ", ";
    }
    private string GetDeserializationMethodName(CodeTypeBase propType, CodeClass parentClass)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyTypeName = conventions.GetTypeString(propType, parentClass, false, false, false);
        var propertyTypeNameWithoutImportSymbol = conventions.TranslateType(propType, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"GetCollectionOfPrimitiveValues(\"{propertyTypeName.ToLowerInvariant()}\")";
                else if (currentType.TypeDefinition is CodeEnum)
                    return $"GetCollectionOfEnumValues({conventions.GetImportedStaticMethodName(propType, parentClass, "Parse")})";
                else
                    return $"GetCollectionOfObjectValues({GetTypeFactory(propType, parentClass, propertyTypeNameWithoutImportSymbol)})";
            if (currentType.TypeDefinition is CodeEnum)
            {
                return $"GetEnumValue({conventions.GetImportedStaticMethodName(propType, parentClass, "Parse")})";
            }
        }
        return propertyTypeNameWithoutImportSymbol switch
        {
            _ when conventions.IsPrimitiveType(propertyTypeNameWithoutImportSymbol) =>
                $"Get{propertyTypeNameWithoutImportSymbol.ToFirstCharacterUpperCase()}Value()",
            _ when conventions.StreamTypeName.Equals(propertyTypeNameWithoutImportSymbol, StringComparison.OrdinalIgnoreCase) =>
                "GetByteArrayValue()",
            _ => $"GetObjectValue({GetTypeFactory(propType, parentClass, propertyTypeNameWithoutImportSymbol)})",
        };
    }

    private string GetTypeFactory(CodeTypeBase propTypeBase, CodeClass parentClass, string propertyTypeName)
    {
        if (propTypeBase is CodeType propType)
            return $"{conventions.GetImportedStaticMethodName(propType, parentClass, "Create", "FromDiscriminatorValue", "able")}";
        return GetTypeFactory(propTypeBase.AllTypes.First(), parentClass, propertyTypeName);
    }
    private void WriteSerializationMethodCall(CodeTypeBase propType, CodeElement parentBlock, string serializationKey, string valueGet, bool shouldDeclareErrorVar, LanguageWriter writer, bool addBlockForErrorScope = true)
    {
        serializationKey = $"\"{serializationKey}\"";
        var errorPrefix = $"err {errorVarDeclaration(shouldDeclareErrorVar)}= writer.";
        var isEnum = propType is CodeType eType && eType.TypeDefinition is CodeEnum;
        var isComplexType = propType is CodeType cType && (cType.TypeDefinition is CodeClass || cType.TypeDefinition is CodeInterface || cType.Name.Equals(GoRefiner.UntypedNodeName, StringComparison.OrdinalIgnoreCase));
        var isInterface = propType is CodeType iType && iType.TypeDefinition is CodeInterface;
        if (addBlockForErrorScope)
            if (isEnum || propType.IsCollection)
                writer.StartBlock($"if {valueGet} != nil {{");
            else
                writer.StartBlock();// so the err var scope is limited
        if (isEnum && !propType.IsCollection)
            writer.WriteLine($"cast := (*{valueGet}).String()");
        else if (isComplexType && propType.IsCollection)
        {
            var parsableSymbol = GetConversionHelperMethodImport(parentBlock, "Parsable");
            writer.WriteLines($"cast := make([]{parsableSymbol}, len({valueGet}))",
                $"for i, v := range {valueGet} {{");
            writer.IncreaseIndent();
            writer.StartBlock("if v != nil {");
            if (isInterface)
                writer.WriteLine($"cast[i] = {GetTypeAssertion("v", parsableSymbol)}");
            else
                writer.WriteLines("temp := v", // temporary creating a new reference to avoid pointers to the same object
                    $"cast[i] = {parsableSymbol}(&temp)");
            writer.CloseBlock();
            writer.CloseBlock();
        }
        var collectionPrefix = propType.IsCollection ? "CollectionOf" : string.Empty;
        var collectionSuffix = propType.IsCollection ? "s" : string.Empty;
        var propertyTypeName = conventions.GetTypeString(propType, parentBlock, false, false, false)
                                .Split('.')
                                .Last()
                                .ToFirstCharacterUpperCase();
        var reference = (isEnum, isComplexType, propType.IsCollection) switch
        {
            (true, false, false) => "&cast",
            (true, false, true) => $"{conventions.GetTypeString(propType, parentBlock, false, false).Replace(propertyTypeName, "Serialize" + propertyTypeName, StringComparison.Ordinal)}({valueGet})", //importSymbol.SerializeEnumName
            (false, true, true) => "cast",
            (_, _, _) => valueGet,
        };
        if (isComplexType)
            propertyTypeName = "Object";
        else if (isEnum)
            propertyTypeName = "String";
        else if (conventions.StreamTypeName.Equals(propertyTypeName, StringComparison.OrdinalIgnoreCase))
            propertyTypeName = "ByteArray";
        writer.WriteLine($"{errorPrefix}Write{collectionPrefix}{propertyTypeName}Value{collectionSuffix}({serializationKey}, {reference})");
        WriteReturnError(writer);
        if (addBlockForErrorScope)
            writer.CloseBlock();
    }
    private string GetConversionHelperMethodImport(CodeElement parentBlock, string name)
    {
        var conversionMethodType = new CodeType { Name = name, IsExternal = true };
        return conventions.GetTypeString(conversionMethodType, parentBlock, true, false);
    }
}
