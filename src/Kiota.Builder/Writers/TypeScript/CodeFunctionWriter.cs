using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using static Kiota.Builder.Refiners.TypeScriptRefiner;
using static Kiota.Builder.Writers.TypeScript.TypeScriptConventionService;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeFunctionWriter(TypeScriptConventionService conventionService) : BaseElementWriter<CodeFunction, TypeScriptConventionService>(conventionService)
{
    /// <summary>
    /// Sorts CodeType objects by inheritance hierarchy.
    /// Derived classes come before their base classes to ensure proper deserialization.
    /// </summary>
    private static List<CodeType> SortTypesByInheritance(IEnumerable<CodeType> types)
    {
        ArgumentNullException.ThrowIfNull(types);

        var typesList = types.ToList();
        if (typesList.Count <= 1)
            return typesList;

        // Helper function to check if type1 derives from type2
        bool DerivesFrom(CodeType type1, CodeType type2)
        {
            // Handle both CodeClass and CodeInterface (TypeScript uses interfaces)
            if (type1.TypeDefinition is CodeClass class1 && type2.TypeDefinition is CodeClass class2)
            {
                return class1.DerivesFrom(class2);
            }
            else if (type1.TypeDefinition is CodeInterface interface1 && type2.TypeDefinition is CodeInterface interface2)
            {
                // For interfaces, check if interface1's OriginalClass derives from interface2's OriginalClass
                if (interface1.OriginalClass != null && interface2.OriginalClass != null)
                {
                    return interface1.OriginalClass.DerivesFrom(interface2.OriginalClass);
                }
            }
            return false;
        }

        // Sort by inheritance: derived classes before base classes
        // Use a topological sort where we prioritize "leaf" classes (most derived)
        var sorted = new List<CodeType>();
        var processed = new HashSet<CodeType>();

        // Keep processing until all types are sorted
        while (processed.Count < typesList.Count)
        {
            var addedInThisIteration = false;

            foreach (var type in typesList)
            {
                if (processed.Contains(type))
                    continue;

                // Check if there are any unprocessed types that derive from this type
                var hasUnprocessedDerivedTypes = typesList.Any(t =>
                    !processed.Contains(t) && t != type && DerivesFrom(t, type));

                // Add this type if no unprocessed derived types exist
                // This ensures derived classes are added before their base classes
                if (!hasUnprocessedDerivedTypes)
                {
                    sorted.Add(type);
                    processed.Add(type);
                    addedInThisIteration = true;
                }
            }

            // If we didn't add anything in this iteration, break to avoid infinite loop
            // This handles cases where there are circular dependencies or no inheritance relationships
            if (!addedInThisIteration)
            {
                // Add remaining types in their original order
                sorted.AddRange(typesList.Where(t => !processed.Contains(t)));
                break;
            }
        }

        return sorted;
    }

    private static readonly HashSet<string> customSerializationWriters = new(StringComparer.OrdinalIgnoreCase) { "writeObjectValue", "writeCollectionOfObjectValues" };
    private const string FactoryMethodReturnType = "((instance?: Parsable) => Record<string, (node: ParseNode) => void>)";

    public override void WriteCodeElement(CodeFunction codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.OriginalLocalMethod == null) throw new InvalidOperationException($"{nameof(codeElement.OriginalLocalMethod)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is not CodeFile parentFile) throw new InvalidOperationException("the parent of a function should be a file");

        var codeMethod = codeElement.OriginalLocalMethod;
        var composedType = GetOriginalComposedType(codeMethod.ReturnType);
        var isComposedOfPrimitives = composedType is not null && composedType.IsComposedOfPrimitives(IsPrimitiveType);

        var returnType = codeMethod.Kind is CodeMethodKind.Factory && !isComposedOfPrimitives ?
            FactoryMethodReturnType :
            GetTypescriptTypeString(codeMethod.ReturnType, codeElement, inlineComposedTypeString: true);
        var isVoid = "void".EqualsIgnoreCase(returnType);
        var codeFile = codeElement.GetImmediateParentOfType<CodeFile>();
        CodeMethodWriter.WriteMethodDocumentationInternal(codeFile, codeElement.OriginalLocalMethod, writer, isVoid, conventions);
        CodeMethodWriter.WriteMethodTypecheckIgnoreInternal(codeElement.OriginalLocalMethod, writer);
        CodeMethodWriter.WriteMethodPrototypeInternal(codeElement.OriginalLocalMethod, writer, returnType, isVoid, conventions, true);

        writer.IncreaseIndent();

        switch (codeMethod.Kind)
        {
            case CodeMethodKind.Deserializer:
                WriteDeserializerFunction(codeElement, codeFile, writer);
                break;
            case CodeMethodKind.Serializer:
                WriteSerializerFunction(codeElement, writer);
                break;
            case CodeMethodKind.Factory:
                WriteFactoryMethod(codeElement, codeFile, writer);
                break;
            case CodeMethodKind.FactoryWithErrorMessage:
                WriteFactoryMethodForErrorClassWithMessage(codeElement, writer);
                break;
            case CodeMethodKind.ClientConstructor:
                WriteApiConstructorBody(parentFile, codeMethod, writer);
                break;
            default: throw new InvalidOperationException("Invalid code method kind");
        }
    }

    private string GetSerializationMethodsForPrimitiveUnionTypes(CodeComposedTypeBase composedType, string parseNodeParameterName, CodeFile codeFile, bool nodeParameterCanBeNull = true)
    {
        var optionalChainingSymbol = nodeParameterCanBeNull ? "?" : string.Empty;
        return string.Join(" ?? ", composedType.Types.Where(x => IsPrimitiveType(x, composedType)).Select(x => $"{parseNodeParameterName}{optionalChainingSymbol}." + conventions.GetDeserializationMethodName(x, codeFile)));
    }

    private static CodeParameter? GetComposedTypeParameter(CodeFunction codeElement)
    {
        return codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(x => GetOriginalComposedType(x) is not null);
    }

    private void WriteComposedTypeDeserializer(CodeFunction codeElement, LanguageWriter writer, CodeParameter composedParam, CodeFile codeFile)
    {

        if (GetOriginalComposedType(composedParam) is not { } composedType) return;

        writer.StartBlock("return {");
        if (composedType.Types.Any(x => IsPrimitiveType(x, composedType, false)))
        {
            var expression = string.Join(" ?? ", composedType.Types.Where(x => IsPrimitiveType(x, composedType, false)).Select(codeType => $"n.{conventions.GetDeserializationMethodName(codeType, codeFile, composedType.IsCollection)}"));
            writer.WriteLine($"\"\" : n => {{ {composedParam.Name.ToFirstCharacterLowerCase()} = {expression}}},");
        }
        foreach (var mappedType in composedType.Types.Where(x => !IsPrimitiveType(x, composedType, false)))
        {
            var functionName = GetDeserializerFunctionName(codeElement, mappedType);
            var variableName = composedParam.Name.ToFirstCharacterLowerCase();
            var variableType = GetTypescriptTypeString(mappedType, codeElement, includeCollectionInformation: false,
                inlineComposedTypeString: true);

            writer.WriteLine($"...{functionName}({variableName} as {variableType}),");
        }
        writer.CloseBlock();
    }

    private void WriteComposedTypeSerializer(CodeFunction codeElement, LanguageWriter writer, CodeParameter composedParam)
    {
        if (GetOriginalComposedType(composedParam) is not { } composedType) return;

        if (composedType.IsComposedOfPrimitives((x, y) => IsPrimitiveType(x, y, false)))
        {
            var paramName = composedParam.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"if ({paramName} === undefined || {paramName} === null) return;");
            bool isFirst = true;
            foreach (var type in composedType.Types.Where(x => IsPrimitiveType(x, composedType, false)))
            {
                var ifElse = isFirst ? "" : "else ";
                WriteCaseStatementForPrimitiveTypeSerialization(type, "undefined", paramName, codeElement, writer, ifElse);
                isFirst = false;
            }
            return;
        }

        if (composedType is CodeIntersectionType)
        {
            WriteSerializationFunctionForCodeIntersectionType(composedType, composedParam, codeElement, writer);
            return;
        }

        WriteSerializationFunctionForCodeUnionTypes(composedType, composedParam, codeElement, writer);
    }

    private void WriteSerializationFunctionForCodeIntersectionType(CodeComposedTypeBase composedType, CodeParameter composedParam, CodeFunction method, LanguageWriter writer)
    {
        foreach (var mappedType in composedType.Types.Where(x => !IsPrimitiveType(x, composedType) && x.TypeDefinition != null))
        {
            var functionName = GetSerializerFunctionName(method, mappedType);
            var variableName = composedParam.Name.ToFirstCharacterLowerCase();
            var variableType = GetTypescriptTypeString(mappedType, method, includeCollectionInformation: false, inlineComposedTypeString: true);

            writer.WriteLine($"{functionName}(writer, {variableName} as {variableType});");
        }
    }

    private void WriteSerializationFunctionForCodeUnionTypes(CodeComposedTypeBase composedType, CodeParameter composedParam, CodeFunction codeElement, LanguageWriter writer)
    {
        var discriminatorInfo = codeElement.OriginalMethodParentClass.DiscriminatorInformation;
        var discriminatorPropertyName = discriminatorInfo.DiscriminatorPropertyName;

        if (string.IsNullOrEmpty(discriminatorPropertyName))
        {
            WriteBruteForceSerializationFunctionForCodeUnionType(composedType, composedParam, codeElement, writer);
            return;
        }

        var paramName = composedParam.Name.ToFirstCharacterLowerCase();
        writer.WriteLine($"if ({paramName} === undefined || {paramName} === null) return;");
        WriteDiscriminatorSwitchBlock(discriminatorInfo, paramName, codeElement, writer);
    }

    /// <summary>
    /// Writes the brute-force serialization function for a union type.
    /// </summary>
    /// <param name="composedType">The composed type representing the union.</param>
    /// <param name="composedParam">The parameter associated with the composed type.</param>
    /// <param name="codeElement">The function code element where serialization is performed.</param>
    /// <param name="writer">The language writer used to generate the code.</param>
    /// <remarks>
    /// This method handles serialization for union types when the discriminator property is missing. 
    /// In the absence of a discriminator, all possible types in the union are serialized. For example, 
    /// a Pet union defined as Cat | Dog would result in the serialization of both Cat and Dog types. 
    /// It delegates the task to the method responsible for intersection types, treating the union 
    /// similarly to an intersection in this context.
    /// </remarks>
    private void WriteBruteForceSerializationFunctionForCodeUnionType(CodeComposedTypeBase composedType, CodeParameter composedParam, CodeFunction codeElement, LanguageWriter writer)
    {
        // Delegate the serialization logic to the method handling intersection types,
        // as both require serializing all possible type variations.
        WriteSerializationFunctionForCodeIntersectionType(composedType, composedParam, codeElement, writer);
    }

    private void WriteDiscriminatorSwitchBlock(DiscriminatorInformation discriminatorInfo, string paramName, CodeFunction codeElement, LanguageWriter writer)
    {
        writer.StartBlock($"switch ({paramName}.{discriminatorInfo.DiscriminatorPropertyName.CleanupSymbolName()}) {{");

        foreach (var mappedType in discriminatorInfo.DiscriminatorMappings)
        {
            writer.StartBlock($"case \"{mappedType.Key}\":");
            writer.WriteLine($"{GetSerializerFunctionName(codeElement, mappedType.Value)}(writer, {paramName} as {mappedType.Value.AllTypes.First().Name.ToFirstCharacterUpperCase()});");
            writer.WriteLine("break;");
            writer.DecreaseIndent();
        }

        writer.CloseBlock();
    }

    private void WriteCaseStatementForPrimitiveTypeSerialization(CodeTypeBase type, string key, string modelParamName, CodeFunction method, LanguageWriter writer, String prefix)
    {
        var nodeType = conventions.GetTypeString(type, method, false);
        var serializationName = GetSerializationMethodName(type, method.OriginalLocalMethod);
        if (string.IsNullOrEmpty(serializationName) || string.IsNullOrEmpty(nodeType)) return;

        writer.StartBlock(type.IsCollection
            ? $"{prefix}if (Array.isArray({modelParamName}) && ({modelParamName}).every(item => typeof item === '{nodeType}')) {{"
            : $"{prefix}if (typeof {modelParamName} === \"{nodeType}\" ) {{");

        writer.WriteLine($"writer.{serializationName}({key}, {modelParamName} as {conventions.GetTypeString(type, method)});");
        writer.CloseBlock();
    }

    private static void WriteApiConstructorBody(CodeFile parentFile, CodeMethod method, LanguageWriter writer)
    {
        if (method.Parameters.OfKind(CodeParameterKind.RequestAdapter)?.Name.ToFirstCharacterLowerCase() is not string
            requestAdapterArgumentName) return;
        writer.StartBlock($"if ({requestAdapterArgumentName} === undefined) {{");
        writer.WriteLine($"throw new Error(\"{requestAdapterArgumentName} cannot be undefined\");");
        writer.CloseBlock();

        writer.WriteLine($"const serializationWriterFactory = {requestAdapterArgumentName}.getSerializationWriterFactory() as SerializationWriterFactoryRegistry;");
        writer.WriteLine($"const parseNodeFactoryRegistry = {requestAdapterArgumentName}.getParseNodeFactory() as ParseNodeFactoryRegistry;");
        writer.WriteLine($"const backingStoreFactory = {requestAdapterArgumentName}.getBackingStoreFactory();");
        writer.WriteLine(string.Empty);

        writer.StartBlock("if (parseNodeFactoryRegistry.registerDefaultDeserializer) {");
        WriteSerializationRegistration(method.DeserializerModules, writer, "parseNodeFactoryRegistry",
            "registerDefaultDeserializer", "backingStoreFactory");
        writer.CloseBlock();
        writer.WriteLine(string.Empty);

        writer.StartBlock("if (serializationWriterFactory.registerDefaultSerializer) {");
        WriteSerializationRegistration(method.SerializerModules, writer, "serializationWriterFactory",
            "registerDefaultSerializer");
        writer.CloseBlock();
        writer.WriteLine(string.Empty);

        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock(
                $"if ({requestAdapterArgumentName}.baseUrl === undefined || {requestAdapterArgumentName}.baseUrl === null || {requestAdapterArgumentName}.baseUrl === \"\") {{");
            writer.WriteLine($"{requestAdapterArgumentName}.baseUrl = \"{method.BaseUrl}\";");
            writer.CloseBlock();
        }

        writer.StartBlock($"const pathParameters: Record<string, unknown> = {{");
        writer.WriteLine($"\"baseurl\": {requestAdapterArgumentName}.baseUrl,");
        writer.CloseBlock("};");
        if (method.Parameters.OfKind(CodeParameterKind.BackingStore)?.Name is string backingStoreParameterName)
            writer.WriteLine(
                $"{requestAdapterArgumentName}.enableBackingStore({backingStoreParameterName.ToFirstCharacterLowerCase()});");
        if (parentFile.Interfaces.FirstOrDefault(static x => x.Kind is CodeInterfaceKind.RequestBuilder) is
            CodeInterface codeInterface)
        {
            var navigationMetadataConstantName =
                parentFile.FindChildByName<CodeConstant>(
                    $"{codeInterface.Name.ToFirstCharacterUpperCase()}{CodeConstant.NavigationMetadataSuffix}",
                    false) is { } navConstant
                    ? navConstant.Name.ToFirstCharacterUpperCase()
                    : "undefined";
            var requestsMetadataConstantName =
                parentFile.FindChildByName<CodeConstant>(
                        $"{codeInterface.Name.ToFirstCharacterUpperCase()}{CodeConstant.RequestsMetadataSuffix}",
                        false) is
                { } reqConstant
                    ? reqConstant.Name.ToFirstCharacterUpperCase()
                    : "undefined";
            writer.WriteLine(
                $"return apiClientProxifier<{codeInterface.Name.ToFirstCharacterUpperCase()}>({requestAdapterArgumentName}, pathParameters, {navigationMetadataConstantName}, {requestsMetadataConstantName});");
        }
    }
    private static void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, string objectName, string methodName, params string[] additionalParam)
    {
        if (serializationModules != null)
            foreach (var module in serializationModules)
                writer.WriteLine($"{objectName}.{methodName}({module}{(additionalParam != null && additionalParam.Length > 0 ? ", " + string.Join(", ", additionalParam) : string.Empty)});");
    }

    private void WriteFactoryMethodForErrorClassWithMessage(CodeFunction codeElement, LanguageWriter writer)
    {
        var messageParam = codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(static p => p.IsOfKind(CodeParameterKind.ErrorMessage));
        if (messageParam != null)
        {
            writer.WriteLine($"return new {codeElement.OriginalMethodParentClass.Name.ToFirstCharacterUpperCase()}({messageParam.Name});");
        }
        else
        {
            writer.WriteLine($"return new {codeElement.OriginalMethodParentClass.Name.ToFirstCharacterUpperCase()}();");
        }
    }

    private void WriteFactoryMethod(CodeFunction codeElement, CodeFile codeFile, LanguageWriter writer)
    {
        var returnType = conventions.GetTypeString(codeElement.OriginalLocalMethod.ReturnType, codeElement);

        if (codeElement.OriginalMethodParentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
            WriteDefensiveStatements(codeElement.OriginalLocalMethod, writer);
        WriteFactoryMethodBody(codeElement, returnType, codeFile, writer);
    }

    private void WriteFactoryMethodBody(CodeFunction codeElement, string returnType, CodeFile codeFile, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.OriginalLocalMethod.Parameters.OfKind(CodeParameterKind.ParseNode);
        var composedType = GetOriginalComposedType(codeElement.OriginalLocalMethod.ReturnType);

        switch (composedType)
        {
            case CodeComposedTypeBase type when type.IsComposedOfPrimitives(IsPrimitiveType):
                string primitiveValuesUnionString = GetSerializationMethodsForPrimitiveUnionTypes(composedType, parseNodeParameter!.Name.ToFirstCharacterLowerCase(), codeFile);
                writer.WriteLine($"return {primitiveValuesUnionString};");
                break;
            case CodeUnionType _ when parseNodeParameter != null:
                WriteDiscriminatorInformation(codeElement, parseNodeParameter, writer);
                // The default discriminator is useful when the discriminator information is not provided.
                WriteDefaultDiscriminator(codeElement, returnType, writer);
                break;
            case CodeIntersectionType _ when parseNodeParameter != null:
                WriteDefaultDiscriminator(codeElement, returnType, writer);
                break;
            default:
                if (codeElement.OriginalMethodParentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType && parseNodeParameter != null)
                    WriteDiscriminatorInformation(codeElement, parseNodeParameter, writer);

                WriteDefaultDiscriminator(codeElement, returnType, writer);
                break;
        }
    }

    private void WriteDefaultDiscriminator(CodeFunction codeElement, string returnType, LanguageWriter writer)
    {
        var nameSpace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        var deserializationFunction = GetFunctionName(codeElement, returnType, CodeMethodKind.Deserializer, nameSpace);
        writer.WriteLine($"return {deserializationFunction.ToFirstCharacterLowerCase()};");
    }

    private void WriteDiscriminatorInformation(CodeFunction codeElement, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        var discriminatorInfo = codeElement.OriginalMethodParentClass.DiscriminatorInformation;
        var discriminatorPropertyName = discriminatorInfo.DiscriminatorPropertyName;

        if (!string.IsNullOrEmpty(discriminatorPropertyName))
        {
            writer.WriteLines($"const mappingValueNode = {parseNodeParameter.Name.ToFirstCharacterLowerCase()}?.getChildNode(\"{discriminatorPropertyName}\");",
                                "if (mappingValueNode) {");
            writer.IncreaseIndent();
            writer.WriteLines("const mappingValue = mappingValueNode.getStringValue();",
                            "if (mappingValue) {");
            writer.IncreaseIndent();

            writer.StartBlock("switch (mappingValue) {");
            foreach (var mappedType in discriminatorInfo.DiscriminatorMappings)
            {
                writer.StartBlock($"case \"{mappedType.Key}\":");
                writer.WriteLine($"return {GetDeserializerFunctionName(codeElement, mappedType.Value)};");
                writer.DecreaseIndent();
            }
            writer.CloseBlock();
            writer.CloseBlock();
            writer.CloseBlock();
        }
    }

    private string GetFunctionName(CodeElement codeElement, string returnType, CodeMethodKind kind, CodeNamespace targetNamespace)
    {
        var functionName = kind switch
        {
            CodeMethodKind.Serializer => $"serialize{returnType}",
            CodeMethodKind.Deserializer => $"deserializeInto{returnType}",
            _ => throw new InvalidOperationException($"Unsupported function kind :: {kind}")
        };

        var codeFunction = FindCodeFunctionInParentNamespaces(functionName, targetNamespace);

        return conventions.GetTypeString(new CodeType { TypeDefinition = codeFunction }, codeElement, false);
    }

    private static CodeFunction? FindCodeFunctionInParentNamespaces(string functionName, CodeNamespace? parentNamespace)
    {
        CodeFunction? codeFunction = null;

        for (var currentNamespace = parentNamespace;
            currentNamespace is not null && !functionName.Equals(codeFunction?.Name, StringComparison.Ordinal);
            currentNamespace = currentNamespace.Parent?.GetImmediateParentOfType<CodeNamespace>())
        {
            codeFunction = currentNamespace.FindChildByName<CodeFunction>(functionName);
        }

        return codeFunction;
    }

    private string GetDeserializerFunctionName(CodeElement codeElement, CodeType returnType) =>
        FindFunctionInNameSpace($"deserializeInto{returnType.Name.ToFirstCharacterUpperCase()}", codeElement, returnType);

    private string GetSerializerFunctionName(CodeElement codeElement, CodeType returnType) =>
        FindFunctionInNameSpace($"serialize{returnType.Name.ToFirstCharacterUpperCase()}", codeElement, returnType);

    private string FindFunctionInNameSpace(string functionName, CodeElement codeElement, CodeType returnType)
    {
        var myNamespace = returnType.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>() ?? throw new InvalidOperationException("Namespace not found for return type");

        CodeFunction[] codeFunctions = [.. myNamespace.FindChildrenByName<CodeFunction>(functionName)];

        var codeFunction = Array.Find(codeFunctions,
            func => func.GetImmediateParentOfType<CodeNamespace>().Name == myNamespace.Name) ??
            throw new InvalidOperationException($"Function {functionName} not found in namespace {myNamespace.Name}");

        var targetElement = codeElement.GetImmediateParentOfType<CodeFile>();

        return GetTypescriptTypeString(new CodeType { TypeDefinition = codeFunction }, targetElement, includeCollectionInformation: false);
    }

    private void WriteSerializerFunction(CodeFunction codeElement, LanguageWriter writer)
    {
        // Determine if the function serializes a composed type
        var composedParam = GetComposedTypeParameter(codeElement);
        if (composedParam is not null)
        {
            WriteComposedTypeSerializer(codeElement, writer, composedParam);
            return;
        }

        if (codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(static x => x.Type is CodeType type && type.TypeDefinition is CodeInterface) is not
            {
                Type: CodeType
                {
                    TypeDefinition: CodeInterface codeInterface
                }
            } param)
            throw new InvalidOperationException("Interface parameter not found for code interface");

        var serializingDerivedTypeParam = codeElement.OriginalLocalMethod.Parameters.FirstOrDefault(static x => x.Kind is CodeParameterKind.SerializingDerivedType) ?? throw new InvalidOperationException("Serializing derived type parameter not found");
        writer.WriteLine($"if (!{param.Name.ToFirstCharacterLowerCase()} || {serializingDerivedTypeParam.Name}) {{ return; }}");
        if (codeInterface.StartBlock.Implements.FirstOrDefault(static x => x.TypeDefinition is CodeInterface) is CodeType inherits)
        {
            writer.WriteLine($"{GetSerializerFunctionName(codeElement, inherits)}(writer, {param.Name.ToFirstCharacterLowerCase()}, {serializingDerivedTypeParam.Name})");
        }

        foreach (var otherProp in codeInterface.Properties.Where(static x => x.IsOfKind(CodePropertyKind.Custom) && !x.ExistsInBaseType && !x.ReadOnly))
        {
            WritePropertySerializer(codeInterface.Name.ToFirstCharacterLowerCase(), otherProp, writer, codeElement);
        }

        if (codeInterface.GetPropertyOfKind(CodePropertyKind.AdditionalData) is CodeProperty additionalDataProperty)
            writer.WriteLine($"writer.writeAdditionalData({codeInterface.Name.ToFirstCharacterLowerCase()}.{additionalDataProperty.Name.ToFirstCharacterLowerCase()});");

        if (codeElement.OriginalMethodParentClass.DiscriminatorInformation.HasBasicDiscriminatorInformation &&
            FindDiscriminatorPropertyBySerializationName(codeInterface, codeElement.OriginalMethodParentClass.DiscriminatorInformation.DiscriminatorPropertyName) is { } discriminatorProperty)
        {
            writer.StartBlock($"switch ({param.Name.ToFirstCharacterLowerCase()}.{discriminatorProperty.Name.ToFirstCharacterLowerCase()}) {{");
            foreach (var mapping in codeElement.OriginalMethodParentClass.DiscriminatorInformation.DiscriminatorMappings)
            {
                var mappedType = mapping.Value;
                writer.StartBlock($"case \"{mapping.Key}\":");
                writer.WriteLine($"{GetSerializerFunctionName(codeElement, mappedType)}(writer, {param.Name.ToFirstCharacterLowerCase()}, true);");
                writer.CloseBlock("break;");
            }
            writer.CloseBlock();
        }
    }
    private static CodeProperty? FindDiscriminatorPropertyBySerializationName(CodeInterface codeInterface, string serializationName)
    {
        if (string.IsNullOrEmpty(serializationName)) return null;
        if (codeInterface.Properties.FirstOrDefault(prop => prop.WireName.EqualsIgnoreCase(serializationName)) is { } discriminatorProperty)
            return discriminatorProperty;
        if (codeInterface.StartBlock.Implements.Any())
            return codeInterface.StartBlock.Implements
                .Select(x => x.TypeDefinition)
                .OfType<CodeInterface>()
                .Select(x => FindDiscriminatorPropertyBySerializationName(x, serializationName))
                .FirstOrDefault(x => x is not null);
        return null;
    }

    private static bool IsCollectionOfEnum(CodeProperty property)
    {
        return property.Type is CodeType codeType && codeType.IsCollection && codeType.TypeDefinition is CodeEnum;
    }

    private void WritePropertySerializer(string modelParamName, CodeProperty codeProperty, LanguageWriter writer, CodeFunction codeFunction)
    {
        var codePropertyName = codeProperty.Name.ToFirstCharacterLowerCase();
        var propTypeName = GetTypescriptTypeString(codeProperty.Type, codeProperty.Parent!, false, inlineComposedTypeString: true);

        var serializationName = GetSerializationMethodName(codeProperty.Type, codeFunction.OriginalLocalMethod);
        var defaultValueSuffix = GetDefaultValueLiteralForProperty(codeProperty) is string dft && !string.IsNullOrEmpty(dft) && !dft.EqualsIgnoreCase("\"null\"") ? $" ?? {dft}" : string.Empty;

        if (customSerializationWriters.Contains(serializationName) && codeProperty.Type is CodeType propType && propType.TypeDefinition is not null)
        {
            var serializeName = GetSerializerAlias(propType, codeFunction, $"serialize{propType.TypeDefinition.Name}");
            if (GetOriginalComposedType(propType.TypeDefinition) is { } ct && (ct.IsComposedOfPrimitives(IsPrimitiveType) || ct.IsComposedOfObjectsAndPrimitives(IsPrimitiveType)))
                WriteSerializationStatementForComposedTypeProperty(ct, modelParamName, codeFunction, writer, codeProperty, serializeName);
            else
                writer.WriteLine($"writer.{serializationName}<{propTypeName}>(\"{codeProperty.WireName}\", {modelParamName}.{codePropertyName}{defaultValueSuffix}, {serializeName});");
        }
        else
        {
            WritePropertySerializationStatement(codeProperty, modelParamName, serializationName, defaultValueSuffix, codeFunction, writer);
        }
    }

    private void WritePropertySerializationStatement(CodeProperty codeProperty, string modelParamName, string? serializationName, string? defaultValueSuffix, CodeFunction codeFunction, LanguageWriter writer)
    {
        var isCollectionOfEnum = IsCollectionOfEnum(codeProperty);
        var codePropertyName = codeProperty.Name.ToFirstCharacterLowerCase();
        var composedType = GetOriginalComposedType(codeProperty.Type);

        if (isCollectionOfEnum)
            writer.WriteLine($"if({modelParamName}.{codePropertyName})");
        if (composedType is not null && (composedType.IsComposedOfPrimitives(IsPrimitiveType) || composedType.IsComposedOfObjectsAndPrimitives(IsPrimitiveType)))
            WriteSerializationStatementForComposedTypeProperty(composedType, modelParamName, codeFunction, writer, codeProperty, string.Empty);
        else
            writer.WriteLine($"writer.{serializationName}(\"{codeProperty.WireName}\", {modelParamName}.{codePropertyName}{defaultValueSuffix});");
    }

    private void WriteSerializationStatementForComposedTypeProperty(CodeComposedTypeBase composedType, string modelParamName, CodeFunction method, LanguageWriter writer, CodeProperty codeProperty, string? serializeName)
    {
        var defaultValueSuffix = GetDefaultValueLiteralForProperty(codeProperty) is string dft && !string.IsNullOrEmpty(dft) && !dft.EqualsIgnoreCase("\"null\"") ? $" ?? {dft}" : string.Empty;
        WriteComposedTypeIfClause(composedType, method, writer, codeProperty, modelParamName, defaultValueSuffix);
        WriteComposedTypeDefaultClause(composedType, writer, codeProperty, modelParamName, defaultValueSuffix, serializeName);
    }

    private void WriteComposedTypeIfClause(CodeComposedTypeBase composedType, CodeFunction method, LanguageWriter writer, CodeProperty codeProperty, string modelParamName, string defaultValueSuffix)
    {
        var codePropertyName = codeProperty.Name.ToFirstCharacterLowerCase();

        bool isFirst = true;
        foreach (var type in composedType.Types.Where(x => IsPrimitiveType(x, composedType)))
        {
            var isElse = isFirst ? "" : "else ";
            var nodeType = conventions.GetTypeString(type, method, false);
            var serializationName = GetSerializationMethodName(type, method.OriginalLocalMethod);
            if (string.IsNullOrEmpty(serializationName) || string.IsNullOrEmpty(nodeType)) return;

            writer.StartBlock(type.IsCollection
                ? $"{isElse}if (Array.isArray({modelParamName}.{codePropertyName}) && ({modelParamName}.{codePropertyName}).every(item => typeof item === '{nodeType}')) {{"
                : $"{isElse}if ( typeof {modelParamName}.{codePropertyName} === \"{nodeType}\") {{");

            writer.WriteLine($"writer.{serializationName}(\"{codeProperty.WireName}\", {modelParamName}.{codePropertyName}{defaultValueSuffix} as {nodeType});");
            writer.CloseBlock();
            isFirst = false;
        }
    }

    private static void WriteComposedTypeDefaultClause(CodeComposedTypeBase composedType, LanguageWriter writer, CodeProperty codeProperty, string modelParamName, string defaultValueSuffix, string? serializeName)
    {
        var codePropertyName = codeProperty.Name.ToFirstCharacterLowerCase();
        var nonPrimitiveTypes = composedType.Types.Where(x => !IsPrimitiveType(x, composedType)).ToArray();
        if (nonPrimitiveTypes.Length > 0)
        {
            writer.StartBlock("else {");
            foreach (var groupedTypes in nonPrimitiveTypes.GroupBy(static x => x.IsCollection))
            {
                var collectionCodeType = (composedType.Clone() as CodeComposedTypeBase)!;
                collectionCodeType.SetTypes(groupedTypes.ToArray());
                var propTypeName = GetTypescriptTypeString(collectionCodeType!, codeProperty.Parent!, false, inlineComposedTypeString: true);

                var writerFunction = groupedTypes.Key ? "writeCollectionOfObjectValues" : "writeObjectValue";
                var propertyTypes = collectionCodeType.IsNullable ? " | undefined | null" : string.Empty;
                var groupSymbol = groupedTypes.Key ? "[]" : string.Empty;

                writer.WriteLine($"writer.{writerFunction}<{propTypeName}>(\"{codeProperty.WireName}\", {modelParamName}.{codePropertyName}{defaultValueSuffix} as {propTypeName}{groupSymbol}{propertyTypes}, {serializeName});");
            }
            writer.CloseBlock();
        }
    }


    private string GetSerializationMethodName(CodeTypeBase propertyType, CodeMethod method)
    {
        ArgumentNullException.ThrowIfNull(propertyType);
        ArgumentNullException.ThrowIfNull(method);

        var composedType = GetOriginalComposedType(propertyType);
        if (composedType is not null && composedType.IsComposedOfPrimitives(IsPrimitiveType))
            return $"serialize{composedType.Name.ToFirstCharacterUpperCase()}";

        var propertyTypeName = TranslateTypescriptType(propertyType);
        CodeType? currentType = composedType is not null ? GetCodeTypeForComposedType(composedType) : propertyType as CodeType;

        if (currentType != null && !string.IsNullOrEmpty(propertyTypeName))
        {
            var result = GetSerializationMethodNameForCodeType(currentType, propertyTypeName);
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }

        if (propertyTypeName is TYPE_LOWERCASE_STRING or TYPE_LOWERCASE_BOOLEAN or TYPE_NUMBER or TYPE_GUID or TYPE_DATE or TYPE_DATE_ONLY or TYPE_TIME_ONLY or TYPE_DURATION)
            return $"write{propertyTypeName.ToFirstCharacterUpperCase()}Value";

        return "writeObjectValue";
    }

    private static CodeType GetCodeTypeForComposedType(CodeComposedTypeBase composedType)
    {
        ArgumentNullException.ThrowIfNull(composedType);
        return new CodeType
        {
            Name = composedType.Name,
            TypeDefinition = composedType,
            CollectionKind = composedType.CollectionKind
        };
    }

    private string? GetSerializationMethodNameForCodeType(CodeType propType, string propertyType)
    {
        return propType switch
        {
            _ when propType.TypeDefinition is CodeEnum currentEnum && !propType.IsCollection => $"writeEnumValue<{currentEnum.Name.ToFirstCharacterUpperCase()}{(currentEnum.Flags ? "[]" : string.Empty)}>",
            _ when propType.TypeDefinition is CodeEnum currentEnum && propType.IsCollection => $"writeCollectionOfEnumValues<{currentEnum.Name.ToFirstCharacterUpperCase()}>",
            _ when conventions.StreamTypeName.Equals(propertyType, StringComparison.OrdinalIgnoreCase) => "writeByteArrayValue",
            _ when propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None => propType.TypeDefinition == null ? $"writeCollectionOfPrimitiveValues<{propertyType}>" : "writeCollectionOfObjectValues",
            _ => null
        };
    }

    private void WriteDeserializerFunction(CodeFunction codeFunction, CodeFile codeFile, LanguageWriter writer)
    {
        var composedParam = GetComposedTypeParameter(codeFunction);
        if (composedParam is not null)
        {
            WriteComposedTypeDeserializer(codeFunction, writer, composedParam, codeFile);
            return;
        }

        var param = codeFunction.OriginalLocalMethod.Parameters.FirstOrDefault();
        if (param?.Type is CodeType codeType && codeType.TypeDefinition is CodeInterface codeInterface)
        {
            WriteDeserializerFunctionProperties(param, codeInterface, codeFunction, codeFile, writer);
        }
        else
        {
            throw new InvalidOperationException($"Model interface for deserializer function {codeFunction.Name} is not available");
        }
    }

    private void WriteDeserializerFunctionProperties(CodeParameter param, CodeInterface codeInterface, CodeFunction codeFunction, CodeFile codeFile, LanguageWriter writer)
    {
        var properties = codeInterface.Properties.Where(static x => x.IsOfKind(CodePropertyKind.Custom, CodePropertyKind.BackingStore) && !x.ExistsInBaseType);

        writer.StartBlock("return {");
        if (codeInterface.StartBlock.Implements.FirstOrDefault(static x => x.TypeDefinition is CodeInterface) is CodeType type && type.TypeDefinition is CodeInterface inherits)
        {
            writer.WriteLine($"...deserializeInto{inherits.Name.ToFirstCharacterUpperCase()}({param.Name.ToFirstCharacterLowerCase()}),");
        }
        var (primaryErrorMapping, primaryErrorMappingKey) = GetPrimaryErrorMapping(codeFunction, param);

        foreach (var otherProp in properties)
        {
            WritePropertyDeserializationBlock(otherProp, param, primaryErrorMapping, primaryErrorMappingKey, codeFile, writer);
        }

        writer.CloseBlock();
    }

    private static (string, string) GetPrimaryErrorMapping(CodeFunction codeFunction, CodeParameter param)
    {
        var primaryErrorMapping = string.Empty;
        var primaryErrorMappingKey = string.Empty;
        var parentClass = codeFunction.OriginalMethodParentClass;

        if (parentClass.IsErrorDefinition && parentClass.AssociatedInterface is not null && parentClass.AssociatedInterface.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterLowerCase(), static x => x.Name.ToFirstCharacterLowerCase(), "?.") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
        {
            primaryErrorMapping = $" {param.Name.ToFirstCharacterLowerCase()}.message = {param.Name.ToFirstCharacterLowerCase()}.{primaryMessageCodePath} ?? \"\";";
            primaryErrorMappingKey = primaryMessageCodePath.Split("?.", StringSplitOptions.RemoveEmptyEntries)[0];
        }

        return (primaryErrorMapping, primaryErrorMappingKey);
    }

    private void WritePropertyDeserializationBlock(CodeProperty otherProp, CodeParameter param, string primaryErrorMapping, string primaryErrorMappingKey, CodeFile codeFile, LanguageWriter writer)
    {
        var suffix = otherProp.Name.Equals(primaryErrorMappingKey, StringComparison.Ordinal) ? primaryErrorMapping : string.Empty;
        var paramName = param.Name.ToFirstCharacterLowerCase();
        var propName = otherProp.Name.ToFirstCharacterLowerCase();

        if (otherProp.Kind is CodePropertyKind.BackingStore)
        {
            writer.WriteLine($"\"{BackingStoreEnabledKey}\": n => {{ {paramName}.{propName} = true;{suffix} }},");
        }
        else if (GetOriginalComposedType(otherProp.Type) is { } composedType)
        {
            var expression = string.Join(" ?? ", SortTypesByInheritance(composedType.Types).Select(codeType => $"n.{conventions.GetDeserializationMethodName(codeType, codeFile, composedType.IsCollection)}"));
            writer.WriteLine($"\"{otherProp.WireName}\": n => {{ {paramName}.{propName} = {expression};{suffix} }},");
        }
        else
        {
            var objectSerializationMethodName = conventions.GetDeserializationMethodName(otherProp.Type, codeFile);
            var defaultValueSuffix = GetDefaultValueSuffix(otherProp);
            writer.WriteLine($"\"{otherProp.WireName}\": n => {{ {paramName}.{propName} = n.{objectSerializationMethodName}{defaultValueSuffix};{suffix} }},");
        }
    }

    private static string GetDefaultValueSuffix(CodeProperty otherProp)
    {
        var defaultValue = GetDefaultValueLiteralForProperty(otherProp);
        return !string.IsNullOrEmpty(defaultValue) && !defaultValue.EqualsIgnoreCase("\"null\"") ? $" ?? {defaultValue}" : string.Empty;
    }

    private static string GetDefaultValueLiteralForProperty(CodeProperty codeProperty)
    {
        if (string.IsNullOrEmpty(codeProperty.DefaultValue)) return string.Empty;
        if (codeProperty.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum enumDefinition &&
            enumDefinition.CodeEnumObject is not null)
        {
            var codeEnumOption = enumDefinition.Options.First(x =>
                x.SymbolName.Equals(codeProperty.DefaultValue.Trim('"').CleanupSymbolName(),
                    StringComparison.OrdinalIgnoreCase));
            var enumDefault = $"{enumDefinition.CodeEnumObject.Name.ToFirstCharacterUpperCase()}.{codeEnumOption.Name.Trim('"').CleanupSymbolName().ToFirstCharacterUpperCase()}";
            if (!string.IsNullOrEmpty(enumDefault) && !enumDefault.EqualsIgnoreCase("\"null\"") && IsCollectionOfEnum(codeProperty))
            {
                enumDefault = "[" + enumDefault + "]";
            }
            return enumDefault;
        }

        // only string primitive should keep quotes
        return codeProperty.Type.Name.Equals("string", StringComparison.Ordinal) ? codeProperty.DefaultValue : codeProperty.DefaultValue.Trim('"');
    }
    private void WriteDefensiveStatements(CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement.IsOfKind(CodeMethodKind.Setter)) return;

        var isRequestExecutor = codeElement.IsOfKind(CodeMethodKind.RequestExecutor);

        foreach (var parameter in codeElement.Parameters
                                        .Where(x => !x.Optional && !x.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.PathParameters) &&
                                                !(isRequestExecutor && x.IsOfKind(CodeParameterKind.RequestBody)))
                                        .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            if (!"boolean".Equals(conventions.TranslateType(parameter.Type), StringComparison.OrdinalIgnoreCase))
                writer.WriteLine($"if(!{parameterName}) throw new Error(\"{parameterName} cannot be undefined\");");
        }
    }

    private string? GetSerializerAlias(CodeType propType, CodeFunction codeFunction, string propertySerializerName)
    {
        CodeFunction serializationFunction;

        if (GetOriginalComposedType(propType) is not null)
        {
            if (codeFunction.GetImmediateParentOfType<CodeFile>() is not CodeFile functionParentFile ||
                functionParentFile.FindChildByName<CodeFunction>(propertySerializerName, false) is not CodeFunction composedTypeSerializationFunction)
            {
                return string.Empty;
            }
            serializationFunction = composedTypeSerializationFunction;
        }
        else
        {
            if (propType.TypeDefinition?.GetImmediateParentOfType<CodeFile>() is not CodeFile parentFile ||
                parentFile.FindChildByName<CodeFunction>(propertySerializerName, false) is not CodeFunction foundSerializationFunction)
            {
                return string.Empty;
            }
            serializationFunction = foundSerializationFunction;
        }

        return conventions.GetTypeString(new CodeType { TypeDefinition = serializationFunction }, codeFunction, false);
    }
}
