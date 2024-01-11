using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeConstantWriter : BaseElementWriter<CodeConstant, TypeScriptConventionService>
{
    public CodeConstantWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeConstant codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        conventions.WriteLongDescription(codeElement, writer);
        switch (codeElement.Kind)
        {
            case CodeConstantKind.QueryParametersMapper:
                WriteQueryParametersMapperConstant(codeElement, writer);
                break;
            case CodeConstantKind.EnumObject:
                WriteEnumObjectConstant(codeElement, writer);
                break;
            case CodeConstantKind.UriTemplate:
                WriteUriTemplateConstant(codeElement, writer);
                break;
            case CodeConstantKind.RequestsMetadata:
                WriteRequestsMetadataConstant(codeElement, writer);
                break;
            case CodeConstantKind.NavigationMetadata:
                WriteNavigationMetadataConstant(codeElement, writer);
                break;
            default:
                throw new InvalidOperationException($"Invalid constant kind {codeElement.Kind}");
        }
    }

    private void WriteNavigationMetadataConstant(CodeConstant codeElement, LanguageWriter writer)
    {
        if (codeElement.OriginalCodeElement is not CodeClass codeClass) throw new InvalidOperationException("Original CodeElement cannot be null");
        if (codeElement.Parent is not CodeFile parentCodeFile || parentCodeFile.FindChildByName<CodeInterface>(codeElement.Name.Replace(CodeConstant.NavigationMetadataSuffix, string.Empty, StringComparison.Ordinal), false) is not CodeInterface currentInterface)
            throw new InvalidOperationException("Couldn't find the associated interface for the navigation metadata constant");
        var navigationMethods = codeClass.Methods
                                    .Where(static x => x.Kind is CodeMethodKind.IndexerBackwardCompatibility or CodeMethodKind.RequestBuilderWithParameters)
                                    .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                    .ToArray();
        var navigationProperties = codeClass.Properties
                    .Where(static x => x.Kind is CodePropertyKind.RequestBuilder)
                    .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
        if (navigationProperties.Length == 0 && navigationMethods.Length == 0)
            return;

        var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        writer.StartBlock($"export const {codeElement.Name.ToFirstCharacterUpperCase()}: Record<Exclude<keyof {currentInterface.Name.ToFirstCharacterUpperCase()}, KeysToExcludeForNavigationMetadata>, NavigationMetadata> = {{");
        foreach (var navigationMethod in navigationMethods)
        {
            writer.StartBlock($"\"{navigationMethod.Name.ToFirstCharacterLowerCase()}\": {{");
            var requestBuilderName = navigationMethod.ReturnType.Name.ToFirstCharacterUpperCase();
            WriteNavigationMetadataEntry(parentNamespace, writer, requestBuilderName, navigationMethod.Parameters.Where(static x => x.Kind is CodeParameterKind.Path or CodeParameterKind.Custom && !string.IsNullOrEmpty(x.SerializationName)).Select(static x => $"\"{x.SerializationName}\"").ToArray());
            writer.CloseBlock("},");
        }
        foreach (var navigationProperty in navigationProperties)
        {
            writer.StartBlock($"\"{navigationProperty.Name.ToFirstCharacterLowerCase()}\": {{");
            var requestBuilderName = navigationProperty.Type.Name.ToFirstCharacterUpperCase();
            WriteNavigationMetadataEntry(parentNamespace, writer, requestBuilderName);
            writer.CloseBlock("},");
        }
        writer.CloseBlock("};");
    }

    private static void WriteNavigationMetadataEntry(CodeNamespace parentNamespace, LanguageWriter writer, string requestBuilderName, string[]? pathParameters = null)
    {
        if (parentNamespace.FindChildByName<CodeConstant>($"{requestBuilderName}{CodeConstant.UriTemplateSuffix}", 3) is CodeConstant uriTemplateConstant && uriTemplateConstant.Kind is CodeConstantKind.UriTemplate)
            writer.WriteLine($"uriTemplate: {uriTemplateConstant.Name.ToFirstCharacterUpperCase()},");
        if (parentNamespace.FindChildByName<CodeConstant>($"{requestBuilderName}{CodeConstant.RequestsMetadataSuffix}", 3) is CodeConstant requestsMetadataConstant && requestsMetadataConstant.Kind is CodeConstantKind.RequestsMetadata)
            writer.WriteLine($"requestsMetadata: {requestsMetadataConstant.Name.ToFirstCharacterUpperCase()},");
        if (parentNamespace.FindChildByName<CodeConstant>($"{requestBuilderName}{CodeConstant.NavigationMetadataSuffix}", 3) is CodeConstant navigationMetadataConstant && navigationMetadataConstant.Kind is CodeConstantKind.NavigationMetadata)
            writer.WriteLine($"navigationMetadata: {navigationMetadataConstant.Name.ToFirstCharacterUpperCase()},");
        if (pathParameters is { Length: > 0 })
            writer.WriteLine($"pathParametersMappings: [{string.Join(", ", pathParameters)}],");
    }

    private void WriteRequestsMetadataConstant(CodeConstant codeElement, LanguageWriter writer)
    {
        if (codeElement.OriginalCodeElement is not CodeClass codeClass) throw new InvalidOperationException("Original CodeElement cannot be null");
        if (codeClass.Methods
                    .Where(static x => x.Kind is CodeMethodKind.RequestExecutor)
                    .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray() is not { Length: > 0 } executorMethods)
            return;
        writer.StartBlock($"export const {codeElement.Name.ToFirstCharacterUpperCase()}: Record<string, RequestMetadata> = {{");
        foreach (var executorMethod in executorMethods)
        {
            var returnType = conventions.GetTypeString(executorMethod.ReturnType, codeElement);
            var isVoid = "void".EqualsIgnoreCase(returnType);
            var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
            var returnTypeWithoutCollectionSymbol = GetReturnTypeWithoutCollectionSymbol(executorMethod, returnType);
            writer.StartBlock($"\"{executorMethod.Name.ToFirstCharacterLowerCase()}\": {{");
            if (codeClass.Methods.FirstOrDefault(x => x.Kind is CodeMethodKind.RequestGenerator && x.HttpMethod == executorMethod.HttpMethod) is { } generatorMethod &&
                 generatorMethod.AcceptHeaderValue is string acceptHeader && !string.IsNullOrEmpty(acceptHeader))
                writer.WriteLine($"responseBodyContentType: \"{acceptHeader}\",");
            if (executorMethod.ErrorMappings.Any())
            {
                writer.StartBlock("errorMappings: {");
                foreach (var errorMapping in executorMethod.ErrorMappings)
                {
                    writer.WriteLine($"\"{errorMapping.Key.ToUpperInvariant()}\": {GetFactoryMethodName(errorMapping.Value, codeElement, writer)},");
                }
                writer.CloseBlock("} as Record<string, ParsableFactory<Parsable>>,");
            }
            writer.WriteLine($"adapterMethodName: \"{GetSendRequestMethodName(isVoid, isStream, executorMethod.ReturnType.IsCollection, returnTypeWithoutCollectionSymbol)}\",");
            if (!isVoid)
                writer.WriteLine($"responseBodyFactory: {GetTypeFactory(isVoid, isStream, executorMethod, writer)},");
            if (!string.IsNullOrEmpty(executorMethod.RequestBodyContentType))
                writer.WriteLine($"requestBodyContentType: \"{executorMethod.RequestBodyContentType}\",");
            if (executorMethod.Parameters.FirstOrDefault(static x => x.Kind is CodeParameterKind.RequestBody) is CodeParameter requestBody)
            {
                if (GetBodySerializer(requestBody) is string bodySerializer)
                    writer.WriteLine($"requestBodySerializer: {bodySerializer},");
                writer.WriteLine($"requestInformationContentSetMethod: {GetRequestContentSetterMethodName(requestBody)},");
            }
            if (codeElement.Parent is CodeFile parentCodeFile &&
                parentCodeFile.FindChildByName<CodeConstant>(codeElement.Name.Replace(CodeConstant.RequestsMetadataSuffix, $"{executorMethod.Name.ToFirstCharacterUpperCase()}QueryParametersMapper", StringComparison.Ordinal), false) is CodeConstant mapperConstant)
                writer.WriteLine($"queryParametersMapper: {mapperConstant.Name.ToFirstCharacterUpperCase()},");
            writer.CloseBlock("},");
        }
        writer.CloseBlock("};");
    }
    private string GetRequestContentSetterMethodName(CodeParameter requestBody)
    {
        if (requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            return "\"setStreamContent\"";

        if (requestBody.Type is CodeType currentType && (currentType.TypeDefinition is CodeInterface || currentType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)))
            return "\"setContentFromParsable\"";
        return "\"setContentFromScalar\"";
    }
    private string? GetBodySerializer(CodeParameter requestBody)
    {
        if (requestBody.Type is CodeType currentType && (currentType.TypeDefinition is CodeInterface || currentType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)))
        {
            return $"serialize{currentType.Name.ToFirstCharacterUpperCase()}";
        }
        return default;
    }
    private string GetTypeFactory(bool isVoid, bool isStream, CodeMethod codeElement, LanguageWriter writer)
    {
        if (isVoid) return string.Empty;
        var typeName = conventions.TranslateType(codeElement.ReturnType);
        if (isStream || conventions.IsPrimitiveType(typeName)) return $" \"{typeName}\"";
        return $" {GetFactoryMethodName(codeElement.ReturnType, codeElement, writer)}";
    }
    private string GetReturnTypeWithoutCollectionSymbol(CodeMethod codeElement, string fullTypeName)
    {
        if (!codeElement.ReturnType.IsCollection) return fullTypeName;
        var clone = (CodeTypeBase)codeElement.ReturnType.Clone();
        clone.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None;
        return conventions.GetTypeString(clone, codeElement);
    }
    private string GetFactoryMethodName(CodeTypeBase targetClassType, CodeElement currentElement, LanguageWriter writer)
    {
        var returnType = conventions.GetTypeString(targetClassType, currentElement, false, writer);
        var targetClassName = conventions.TranslateType(targetClassType);
        var resultName = $"create{targetClassName.ToFirstCharacterUpperCase()}FromDiscriminatorValue";
        if (targetClassName.Equals(returnType, StringComparison.OrdinalIgnoreCase))
            return resultName;
        if (targetClassType is CodeType currentType &&
            currentType.TypeDefinition is CodeClass definitionClass &&
            definitionClass.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace parentNamespace &&
            parentNamespace.FindChildByName<CodeFunction>(resultName) is CodeFunction factoryMethod)
        {
            var methodName = conventions.GetTypeString(new CodeType
            {
                Name = resultName,
                TypeDefinition = factoryMethod
            }, currentElement, false, writer);
            return methodName.ToFirstCharacterUpperCase();// static function is aliased
        }
        throw new InvalidOperationException($"Unable to find factory method for {targetClassName}");
    }
    private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType)
    {
        if (isVoid) return "sendNoResponseContentAsync";
        if (isCollection)
        {
            if (conventions.IsPrimitiveType(returnType)) return $"sendCollectionOfPrimitiveAsync";
            return $"sendCollectionAsync";
        }

        if (isStream || conventions.IsPrimitiveType(returnType)) return $"sendPrimitiveAsync";
        return $"sendAsync";
    }

    private void WriteUriTemplateConstant(CodeConstant codeElement, LanguageWriter writer)
    {
        writer.WriteLine($"export const {codeElement.Name.ToFirstCharacterUpperCase()} = {codeElement.UriTemplate};");
    }

    private static void WriteQueryParametersMapperConstant(CodeConstant codeElement, LanguageWriter writer)
    {
        if (codeElement.OriginalCodeElement is not CodeInterface codeInterface) throw new InvalidOperationException("Original CodeElement cannot be null");
        if (codeInterface.Properties
                        .OfKind(CodePropertyKind.QueryParameter)
                        .Where(static x => !string.IsNullOrEmpty(x.SerializationName))
                        .OrderBy(static x => x.SerializationName, StringComparer.OrdinalIgnoreCase)
                        .ToArray() is not { Length: > 0 } properties)
            return;
        writer.StartBlock($"const {codeElement.Name.ToFirstCharacterUpperCase()}: Record<string, string> = {{");
        foreach (var property in properties)
        {
            writer.WriteLine($"\"{property.Name.ToFirstCharacterLowerCase()}\": \"{property.SerializationName}\",");
        }
        writer.CloseBlock("};");
    }
    private void WriteEnumObjectConstant(CodeConstant codeElement, LanguageWriter writer)
    {
        if (codeElement.OriginalCodeElement is not CodeEnum codeEnum) throw new InvalidOperationException("Original CodeElement cannot be null");
        if (!codeEnum.Options.Any())
            return;
        conventions.WriteLongDescription(codeEnum, writer);
        writer.StartBlock($"export const {codeElement.Name.ToFirstCharacterUpperCase()} = {{");
        codeEnum.Options.ToList().ForEach(x =>
        {
            conventions.WriteShortDescription(x.Documentation.Description, writer);
            writer.WriteLine($"{x.Name.ToFirstCharacterUpperCase()}: \"{x.WireName}\",");
        });
        writer.CloseBlock("}  as const;");
    }
}
