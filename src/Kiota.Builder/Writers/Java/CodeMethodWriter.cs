using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;

namespace Kiota.Builder.Writers.Java;
public partial class CodeMethodWriter : BaseElementWriter<CodeMethod, JavaConventionService>
{
    public CodeMethodWriter(JavaConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("the parent of a method should be a class");

        var baseReturnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var finalReturnType = GetFinalReturnType(codeElement, baseReturnType);
        WriteMethodDocumentation(codeElement, writer, baseReturnType, finalReturnType);
        WriteMethodPrototype(codeElement, writer, finalReturnType);
        writer.IncreaseIndent();
        var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var configParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestContentType = codeElement.Parameters.OfKind(CodeParameterKind.RequestBodyContentType);
        var requestParams = new RequestParams(requestBodyParam, configParam, requestContentType);
        AddNullChecks(codeElement, writer);
        switch (codeElement.Kind)
        {
            case CodeMethodKind.Serializer:
                WriteSerializerBody(parentClass, codeElement, writer, inherits);
                break;
            case CodeMethodKind.Deserializer:
                WriteDeserializerBody(codeElement, parentClass, writer, inherits);
                break;
            case CodeMethodKind.IndexerBackwardCompatibility:
                WriteIndexerBody(codeElement, parentClass, writer, finalReturnType);
                break;
            case CodeMethodKind.RequestGenerator when codeElement.IsOverload:
                WriteGeneratorOrExecutorMethodCall(codeElement, requestParams, parentClass, writer, "return ", CodeMethodKind.RequestGenerator);
                break;
            case CodeMethodKind.RequestGenerator when !codeElement.IsOverload:
                WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                break;
            case CodeMethodKind.RequestExecutor when codeElement.IsOverload:
                WriteGeneratorOrExecutorMethodCall(codeElement, requestParams, parentClass, writer, "return ", CodeMethodKind.RequestExecutor);
                break;
            case CodeMethodKind.RequestExecutor when !codeElement.IsOverload:
                WriteRequestExecutorBody(codeElement, requestParams, parentClass, writer);
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
                break;
            case CodeMethodKind.Constructor when codeElement.IsOverload && parentClass.IsOfKind(CodeClassKind.RequestBuilder):
                WriteRequestBuilderConstructorCall(codeElement, writer);
                break;
            case CodeMethodKind.Constructor:
            case CodeMethodKind.RawUrlConstructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                break;
            case CodeMethodKind.RequestBuilderWithParameters:
                WriteRequestBuilderBody(parentClass, codeElement, writer);
                break;
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
            case CodeMethodKind.Factory when !codeElement.IsOverload:
                WriteFactoryMethodBody(codeElement, parentClass, writer);
                break;
            case CodeMethodKind.Factory when codeElement.IsOverload:
                WriteFactoryOverloadMethod(codeElement, parentClass, writer);
                break;
            case CodeMethodKind.ErrorMessageOverride:
                WriteErrorMethodOverride(parentClass, writer);
                break;
            case CodeMethodKind.QueryParametersMapper:
                WriteQueryParametersExtractorBody(codeElement, writer, parentClass);
                break;
            case CodeMethodKind.ComposedTypeMarker:
                throw new InvalidOperationException("ComposedTypeMarker is not required as interface is explicitly implemented.");
            default:
                writer.WriteLine("return null;");
                break;
        }
        writer.CloseBlock();
    }
    private static void WriteErrorMethodOverride(CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.IsErrorDefinition && parentClass.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterLowerCase(), static x => x.Name.ToFirstCharacterLowerCase() + "()") is string primaryMessageCodePath && !string.IsNullOrEmpty(primaryMessageCodePath))
        {
            writer.WriteLine($"return this.{primaryMessageCodePath};");
        }
        else
        {
            writer.WriteLine("return super.getMessage();");
        }
    }
    private void WriteRawUrlBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var rawUrlParameter = codeElement.Parameters.OfKind(CodeParameterKind.RawUrl) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RawUrl parameter");
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RequestAdapter property");
        writer.WriteLine($"return new {parentClass.Name}({rawUrlParameter.Name}, {requestAdapterProperty.Name});");
    }
    private const string ResultVarName = "result";
    private void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode) ?? throw new InvalidOperationException("Factory method should have a ParseNode parameter");
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType || parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            writer.WriteLine($"final {parentClass.Name} {ResultVarName} = new {parentClass.Name}();");
        var writeDiscriminatorValueRead = parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck && !parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType;
        if (writeDiscriminatorValueRead)
        {
            writer.WriteLine($"final ParseNode mappingValueNode = {parseNodeParameter.Name}.getChildNode(\"{parentClass.DiscriminatorInformation.DiscriminatorPropertyName}\");");
            writer.StartBlock("if (mappingValueNode != null) {");
            writer.WriteLine($"final String {DiscriminatorMappingVarName} = mappingValueNode.getStringValue();");
        }
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
        {
            int count = 0;
            foreach (var item in parentClass.DiscriminatorInformation.DiscriminatorMappings)
            {
                count++;
                if (count > MaxDiscriminatorsPerMethod) break;
            }

            if (count > MaxDiscriminatorsPerMethod)
                WriteSplitFactoryMethodBodyForInheritedModel(parentClass, writer);
            else
                WriteFactoryMethodBodyForInheritedModel(parentClass.DiscriminatorInformation.DiscriminatorMappings, writer);
        }
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType && parentClass.DiscriminatorInformation.HasBasicDiscriminatorInformation)
            WriteFactoryMethodBodyForUnionModelForDiscriminatedTypes(codeElement, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteFactoryMethodBodyForIntersectionModel(codeElement, parentClass, parseNodeParameter, writer);
        if (writeDiscriminatorValueRead)
        {
            writer.CloseBlock();
        }
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType || parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
        {
            if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
                WriteFactoryMethodBodyForUnionModelForUnDiscriminatedTypes(codeElement, parentClass, parseNodeParameter, writer);
            writer.WriteLine($"return {ResultVarName};");
        }
        else
            writer.WriteLine($"return new {parentClass.Name}();");
    }
    [GeneratedRegex(@"_(?<idx>\d+)", RegexOptions.Singleline, 500)]
    private static partial Regex factoryMethodIndexParser();
    private static void WriteFactoryOverloadMethod(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var enumerator = parentClass.DiscriminatorInformation.DiscriminatorMappings.GetEnumerator();
        var count = 0;
        while (enumerator.MoveNext())
        {
            count++;
        }

        if (int.TryParse(factoryMethodIndexParser().Match(codeElement.Name).Groups["idx"].Value, out var currentDiscriminatorPageIndex) && count > 0)
        {
            CodeParameter? parameter = null;
            foreach (var param in codeElement.Parameters)
            {
                if (param.IsOfKind(CodeParameterKind.Path))
                {
                    parameter = param;
                    break; // Found the first parameter of kind Path
                }
            }

            if (parameter != null)
            {
                var takeValue = Math.Min(MaxDiscriminatorsPerMethod, count - currentDiscriminatorPageIndex * MaxDiscriminatorsPerMethod);
                enumerator.Reset(); // Reset the enumerator to its initial position

                for (var i = 0; i < currentDiscriminatorPageIndex * MaxDiscriminatorsPerMethod; i++)
                {
                    enumerator.MoveNext();
                }

                var currentDiscriminatorPage = new Dictionary<string, CodeType>();
                for (var i = 0; i < takeValue; i++)
                {
                    if (enumerator.MoveNext())
                    {
                        currentDiscriminatorPage.Add(enumerator.Current.Key, enumerator.Current.Value);
                    }
                    else
                    {
                        break;
                    }
                }

                WriteFactoryMethodBodyForInheritedModel(currentDiscriminatorPage, writer, parameter.Name);
            }
        }
        writer.WriteLine("return null;");
    }

    private const int MaxDiscriminatorsPerMethod = 500;
    private static void WriteSplitFactoryMethodBodyForInheritedModel(CodeClass parentClass, LanguageWriter writer)
    {
        var methodNames = new List<string>();
        foreach (var method in parentClass.Methods)
        {
            if (method.IsOverload && method.IsOfKind(CodeMethodKind.Factory))
            {
                methodNames.Add(method.Name);
            }
        }

        methodNames.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (var otherMethodName in methodNames)
        {
            var varName = $"{otherMethodName}_result";
            writer.WriteLine($"final {parentClass.Name} {varName} = {otherMethodName}({DiscriminatorMappingVarName});");
            writer.StartBlock($"if ({varName} != null) {{");
            writer.WriteLine($"return {varName};");
            writer.CloseBlock();
        }
    }
    private static void WriteFactoryMethodBodyForInheritedModel(IEnumerable<KeyValuePair<string, CodeType>> discriminatorMappings, LanguageWriter writer, string varName = "")
    {
        if (string.IsNullOrEmpty(varName))
            varName = DiscriminatorMappingVarName;
        writer.StartBlock($"switch ({varName}) {{");
        foreach (var mappedType in discriminatorMappings)
        {
            CodeType? firstType = null;
            foreach (var type in mappedType.Value.AllTypes)
            {
                firstType = type;
                break;
            }

            if (firstType != null)
            {
                writer.WriteLine($"case \"{mappedType.Key}\": return new {firstType.TypeDefinition?.Name}();");
            }
        }
        writer.CloseBlock();
    }
    private static readonly CodePropertyTypeComparer CodePropertyTypeForwardComparer = new();
    private static readonly CodePropertyTypeComparer CodePropertyTypeBackwardComparer = new(true);
    private void WriteFactoryMethodBodyForIntersectionModel(CodeMethod codeElement, CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = new List<CodeProperty>();
        foreach (var prop in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (prop.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
            {
                otherProps.Add(prop);
            }
        }

        otherProps.Sort((x, y) =>
        {
            int compare = CodePropertyTypeBackwardComparer.Compare(x, y);
            if (compare == 0)
            {
                compare = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }
            return compare;
        });

        foreach (var property in otherProps)
        {
            if (property.Setter != null && property.Type is CodeType propertyType)
            {
                var deserializationMethodName = $"{parseNodeParameter.Name}.{GetDeserializationMethodName(propertyType, codeElement)}";
                writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ({deserializationMethodName} != null) {{");
                writer.WriteLine($"{ResultVarName}.{property.Setter!.Name}({deserializationMethodName});");
                writer.DecreaseIndent();
            }
            if (!includeElse)
                includeElse = true;
        }

        var complexProperties = new List<Tuple<CodeProperty, CodeType>>();
        foreach (var prop in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (prop.Type is CodeType xType && xType.TypeDefinition is CodeClass && !xType.IsCollection)
            {
                complexProperties.Add(new Tuple<CodeProperty, CodeType>(prop, xType));
            }
        }

        if (complexProperties.Count != 0)
        {
            if (includeElse)
                writer.StartBlock("} else {");
            foreach (var property in complexProperties)
            {
                if (property.Item1.Setter != null)
                    writer.WriteLine($"{ResultVarName}.{property.Item1.Setter!.Name}(new {conventions.GetTypeString(property.Item2, codeElement, false)}());");
            }
            if (includeElse)
                writer.CloseBlock();
        }
        else if (otherProps.Count != 0)
            writer.CloseBlock(decreaseIndent: false);
    }
    private const string DiscriminatorMappingVarName = "mappingValue";
    private void WriteFactoryMethodBodyForUnionModelForDiscriminatedTypes(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = new List<CodeProperty>();
        foreach (var prop in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (prop.Type is CodeType xType && !xType.IsCollection && (xType.TypeDefinition is CodeClass || xType.TypeDefinition is CodeInterface))
            {
                otherProps.Add(prop);
            }
        }

        otherProps.Sort((x, y) =>
        {
            int compare = CodePropertyTypeForwardComparer.Compare(x, y);
            if (compare == 0)
            {
                compare = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }
            return compare;
        });

        foreach (var property in otherProps)
        {
            if (property.Setter != null)
            {
                if (property.Type is CodeType propertyType && propertyType.TypeDefinition is CodeInterface typeInterface && typeInterface.OriginalClass != null)
                    propertyType = new CodeType
                    {
                        Name = typeInterface.OriginalClass.Name,
                        TypeDefinition = typeInterface.OriginalClass,
                        CollectionKind = propertyType.CollectionKind,
                        IsNullable = propertyType.IsNullable,
                    };
                KeyValuePair<string, CodeType> mappedType = default;
                foreach (var mapping in parentClass.DiscriminatorInformation.DiscriminatorMappings)
                {
                    if (mapping.Value.Name.Equals(property.Type.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        mappedType = mapping;
                        break;
                    }
                }
                if (!mappedType.Equals(default(KeyValuePair<string, CodeType>)))
                {
                    writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if (\"{mappedType.Key}\".equalsIgnoreCase({DiscriminatorMappingVarName})) {{");
                    writer.WriteLine($"{ResultVarName}.{property.Setter!.Name}(new {conventions.GetTypeString(property.Type, codeElement, false)}());");
                    writer.DecreaseIndent();
                    if (!includeElse)
                        includeElse = true;
                }
            }
        }
        if (otherProps.Count != 0)
            writer.CloseBlock(decreaseIndent: false);
    }
    private void WriteFactoryMethodBodyForUnionModelForUnDiscriminatedTypes(CodeMethod currentElement, CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = new List<CodeProperty>();
        foreach (var prop in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (prop.Type is CodeType xType && (xType.IsCollection || xType.TypeDefinition is null || xType.TypeDefinition is CodeEnum))
            {
                otherProps.Add(prop);
            }
        }

        otherProps.Sort((x, y) =>
        {
            int compare = CodePropertyTypeForwardComparer.Compare(x, y);
            if (compare == 0)
            {
                compare = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }
            return compare;
        });

        foreach (var property in otherProps)
        {
            if (property.Setter != null)
            {
                if (property.Type is CodeType propertyType)
                {
                    var serializationMethodName = $"{parseNodeParameter.Name}.{GetDeserializationMethodName(propertyType, currentElement)}";
                    writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if ({serializationMethodName} != null) {{");
                    writer.WriteLine($"{ResultVarName}.{property.Setter!.Name}({serializationMethodName});");
                    writer.DecreaseIndent();
                    if (!includeElse)
                        includeElse = true;
                }
            }
        }
        if (otherProps.Count != 0)
            writer.CloseBlock(decreaseIndent: false);
    }
    private void WriteRequestBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var importSymbol = conventions.GetTypeString(codeElement.ReturnType, parentClass);
        var pathParameters = new List<CodeParameter>();
        foreach (var parameter in codeElement.Parameters)
        {
            if (parameter.IsOfKind(CodeParameterKind.Path))
            {
                pathParameters.Add(parameter);
            }
        }
        conventions.AddRequestBuilderBody(parentClass, importSymbol, writer, pathParameters: pathParameters);
    }
    private static void AddNullChecks(CodeMethod codeElement, LanguageWriter writer)
    {
        if (!codeElement.IsOverload)
        {
            var parameters = new List<CodeParameter>();
            foreach (var parameter in codeElement.Parameters)
            {
                if (!parameter.Optional && !parameter.IsOfKind(CodeParameterKind.RequestAdapter, CodeParameterKind.PathParameters))
                {
                    parameters.Add(parameter);
                }
            }

            parameters.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

            foreach (var parameter in parameters)
            {
                writer.WriteLine($"Objects.requireNonNull({parameter.Name});");
            }
        }
    }
    private static void WriteRequestBuilderConstructorCall(CodeMethod codeElement, LanguageWriter writer)
    {
        var pathParameters = new List<string>();
        foreach (var parameter in codeElement.Parameters)
        {
            if (parameter.IsOfKind(CodeParameterKind.Path))
            {
                pathParameters.Add(parameter.Name);
            }
        }

        var pathParametersRef = pathParameters.Count != 0 ? (", " + string.Join(", ", pathParameters)) : string.Empty;

        CodeParameter? requestAdapterParameter = null;
        CodeParameter? urlTemplateParamsParameter = null;
        foreach (var parameter in codeElement.Parameters)
        {
            if (parameter.IsOfKind(CodeParameterKind.RequestAdapter))
            {
                requestAdapterParameter = parameter;
            }
            if (parameter.IsOfKind(CodeParameterKind.PathParameters))
            {
                urlTemplateParamsParameter = parameter;
            }
        }

        if (requestAdapterParameter != null && urlTemplateParamsParameter != null)
        {
            writer.WriteLine($"this({urlTemplateParamsParameter.Name}, {requestAdapterParameter.Name}{pathParametersRef});");
        }
    }
    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter);
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        CodeParameter? backingStoreParameter = null;
        foreach (var parameter in method.Parameters)
        {
            if (parameter.IsOfKind(CodeParameterKind.BackingStore))
            {
                backingStoreParameter = parameter;
                break;
            }
        }
        var requestAdapterPropertyName = requestAdapterProperty?.Name ?? string.Empty;
        WriteSerializationRegistration(method.SerializerModules, writer, "registerDefaultSerializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "registerDefaultDeserializer");
        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock($"if ({requestAdapterPropertyName}.getBaseUrl() == null || {requestAdapterPropertyName}.getBaseUrl().isEmpty()) {{");
            writer.WriteLine($"{requestAdapterPropertyName}.setBaseUrl(\"{method.BaseUrl}\");");
            writer.CloseBlock();
            if (pathParametersProperty != null)
                writer.WriteLine($"{pathParametersProperty.Name}.put(\"baseurl\", {requestAdapterPropertyName}.getBaseUrl());");
        }
        if (backingStoreParameter != null)
            writer.WriteLine($"this.{requestAdapterPropertyName}.enableBackingStore({backingStoreParameter.Name});");
    }
    private static void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, string methodName)
    {
        if (serializationModules != null)
            foreach (var module in serializationModules)
                writer.WriteLine($"ApiClientBuilder.{methodName}(() -> new {module}());");
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits)
    {
        if (inherits)
        {
            CodeParameter? requestAdapterParameter = null;
            CodeProperty? urlTemplateProperty = null;
            foreach (var parameter in currentMethod.Parameters)
            {
                if (parameter.IsOfKind(CodeParameterKind.RequestAdapter))
                {
                    requestAdapterParameter = parameter;
                    break;
                }
            }
            foreach (var property in parentClass.Properties)
            {
                if (property.IsOfKind(CodePropertyKind.UrlTemplate))
                {
                    urlTemplateProperty = property;
                    break;
                }
            }
            if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
                requestAdapterParameter != null &&
                urlTemplateProperty != null &&
                !string.IsNullOrEmpty(urlTemplateProperty.DefaultValue))
            {
                var thirdParameterName = string.Empty;
                foreach (var parameter in currentMethod.Parameters)
                {
                    if (parameter.IsOfKind(CodeParameterKind.PathParameters) || parameter.IsOfKind(CodeParameterKind.RawUrl))
                    {
                        thirdParameterName = $", {parameter.Name}";
                        break;
                    }
                }
                writer.WriteLine($"super({requestAdapterParameter.Name}, {urlTemplateProperty.DefaultValue}{thirdParameterName});");
            }
            else
            {
                writer.WriteLine("super();");
            }
        }
        var properties = new List<CodeProperty>();
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.BackingStore,
                                                                CodePropertyKind.RequestBuilder,
                                                                CodePropertyKind.PathParameters))
        {
            if (!string.IsNullOrEmpty(property.DefaultValue))
            {
                properties.Add(property);
            }
        }
        properties.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
        foreach (var propWithDefault in properties)
        {
            writer.WriteLine($"this.{propWithDefault.NamePrefix}{propWithDefault.Name} = {propWithDefault.DefaultValue};");
        }
        properties.Clear();
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.AdditionalData, CodePropertyKind.Custom)) //additional data and custom properties rely on accessors
        {
            if (!string.IsNullOrEmpty(property.DefaultValue) &&
                (property.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null))
            {
                properties.Add(property);
            }
        }
        properties.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
        foreach (var propWithDefault in properties)
        {
            var setterName = propWithDefault.SetterFromCurrentOrBaseType?.Name is string sName && !string.IsNullOrEmpty(sName) ? sName : $"set{propWithDefault.Name.ToFirstCharacterUpperCase()}";
            var defaultValue = propWithDefault.DefaultValue;
            if (propWithDefault.Type is CodeType propertyType && propertyType.TypeDefinition is CodeEnum enumDefinition)
            {
                defaultValue = $"{enumDefinition.Name}.forValue({defaultValue})";
            }
            // avoid setting null as a string.
            if (propWithDefault.Type.IsNullable &&
                defaultValue.TrimQuotes().Equals(NullValueString, StringComparison.OrdinalIgnoreCase))
            {
                defaultValue = NullValueString;
            }
            writer.WriteLine($"this.{setterName}({defaultValue});");
        }
        if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
            parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProp &&
            currentMethod.IsOfKind(CodeMethodKind.Constructor) &&
            currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParam)
        {
            var pathParameters = new List<CodeParameter>();
            foreach (var parameter in currentMethod.Parameters)
            {
                if (parameter.IsOfKind(CodeParameterKind.Path))
                {
                    pathParameters.Add(parameter);
                }
            }
            if (pathParameters.Count != 0)
            {
                var parameters = new List<(CodeTypeBase, string, string)>();
                foreach (var parameter in pathParameters)
                {
                    parameters.Add((parameter.Type, string.IsNullOrEmpty(parameter.SerializationName) ? parameter.Name : parameter.SerializationName, parameter.Name));
                }
                conventions.AddParametersAssignment(writer,
                                                    pathParametersParam.Type,
                                                    pathParametersParam.Name,
                                                    $"this.{pathParametersProp.Name}",
                                                    parameters.ToArray());
            }
        }
    }
    private const string NullValueString = "null";
    private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass)
    {
        if (parentClass.GetBackingStoreProperty() is not CodeProperty backingStore || (codeElement.AccessedProperty?.IsOfKind(CodePropertyKind.BackingStore) ?? false))
        {
            string value = "PeriodAndDuration".Equals(codeElement.AccessedProperty?.Type?.Name, StringComparison.OrdinalIgnoreCase) ? "PeriodAndDuration.ofPeriodAndDuration(value);" : "value;";
            writer.WriteLine($"this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name} = {value}");
        }
        else
            writer.WriteLine($"this.{backingStore.Name}.set(\"{codeElement.AccessedProperty?.Name}\", value);");
    }
    private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass)
    {
        var backingStore = parentClass.GetBackingStoreProperty();
        if (backingStore == null || (codeElement.AccessedProperty?.IsOfKind(CodePropertyKind.BackingStore) ?? false))
            writer.WriteLine($"return this.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name};");
        else
            if (!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                !(codeElement.AccessedProperty?.ReadOnly ?? true) &&
                !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue))
        {
            writer.WriteLine($"{conventions.GetTypeString(codeElement.AccessedProperty.Type, codeElement)} value = this.{backingStore.Name}.get(\"{codeElement.AccessedProperty.Name}\");");
            writer.StartBlock("if(value == null) {");
            writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue};",
                $"this.set{codeElement.AccessedProperty?.Name.ToFirstCharacterUpperCase()}(value);");
            writer.CloseBlock();
            writer.WriteLine("return value;");
        }
        else
            writer.WriteLine($"return this.{backingStore.Name}.get(\"{codeElement.AccessedProperty?.Name}\");");
    }
    private void WriteQueryParametersExtractorBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass)
    {
        writer.WriteLine("final Map<String, Object> allQueryParams = new HashMap();");
        var allQueryParams = new List<CodeProperty>();
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.QueryParameter))
        {
            allQueryParams.Add(property);
        }
        allQueryParams.Sort((x, y) =>
        {
            var compareType = CodePropertyTypeForwardComparer.Compare(x, y);
            if (compareType != 0)
            {
                return compareType;
            }
            return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        });
        foreach (var queryParam in allQueryParams)
        {
            var keyValue = queryParam.IsNameEscaped ? queryParam.SerializationName : queryParam.Name;
            writer.WriteLine($"allQueryParams.put(\"{keyValue}\", {queryParam.Name});");
        }
        writer.WriteLine("return allQueryParams;");
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, string returnType)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty && codeElement.OriginalIndexer != null)
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"this.{pathParametersProperty.Name}",
                parameters: (codeElement.OriginalIndexer.IndexParameter.Type, codeElement.OriginalIndexer.IndexParameter.SerializationName, codeElement.OriginalIndexer.IndexParameter.Name));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName);
    }
    private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, bool inherits)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteDeserializerBodyForUnionModel(codeElement, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteDeserializerBodyForIntersectionModel(parentClass, writer);
        else
            WriteDeserializerBodyForInheritedModel(codeElement, parentClass, writer, inherits);
    }
    private const string DeserializerReturnType = "HashMap<String, java.util.function.Consumer<ParseNode>>";
    private static void WriteDeserializerBodyForUnionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        var otherPropGetters = new List<CodeProperty>();
        var properties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);

        foreach (var property in properties)
        {
            if (!property.ExistsInBaseType && property.Getter != null && property.Type is CodeType propertyType && !propertyType.IsCollection && propertyType.TypeDefinition is CodeClass)
            {
                otherPropGetters.Add(property);
            }
        }

        otherPropGetters.Sort(CodePropertyTypeForwardComparer.Compare);

        foreach (var otherPropGetter in otherPropGetters)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if (this.{otherPropGetter}() != null) {{");
            writer.WriteLine($"return this.{otherPropGetter}().{method.Name}();");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherPropGetters.Count != 0)
            writer.CloseBlock(decreaseIndent: false);
        writer.WriteLine($"return new {DeserializerReturnType}();");
    }
    private static void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = new List<CodeProperty>();
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (property.Type is CodeType propType && propType.TypeDefinition is CodeClass && !property.Type.IsCollection)
            {
                complexProperties.Add(property);
            }
        }
        if (complexProperties.Count != 0)
        {
            var propertiesNames = new List<string>();
            foreach (var property in complexProperties)
            {
                if (property.Getter != null)
                {
                    propertiesNames.Add(property.Getter.Name);
                }
            }
            propertiesNames.Sort();
            var propertiesNamesAsConditions = "";
            var propertiesNamesAsArgument = "";
            for (var i = 0; i < propertiesNames.Count; i++)
            {
                if (i > 0)
                {
                    propertiesNamesAsConditions += " || ";
                    propertiesNamesAsArgument += ", ";
                }
                propertiesNamesAsConditions += $"this.{propertiesNames[i]}() != null";
                propertiesNamesAsArgument += $"this.{propertiesNames[i]}()";
            }
            writer.StartBlock($"if ({propertiesNamesAsConditions}) {{");
            writer.WriteLine($"return ParseNodeHelper.mergeDeserializersForIntersectionWrapper({propertiesNamesAsArgument});");
            writer.CloseBlock();
        }
        writer.WriteLine($"return new {DeserializerReturnType}();");
    }
    private const string DeserializerVarName = "deserializerMap";
    private void WriteDeserializerBodyForInheritedModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer, bool inherits)
    {
        var fieldToSerializeList = new List<CodeProperty>();
        foreach (var prop in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            fieldToSerializeList.Add(prop);
        }
        writer.WriteLines(
            $"final {DeserializerReturnType} {DeserializerVarName} = new {DeserializerReturnType}({(inherits ? "super." + method.Name + "()" : fieldToSerializeList.Count)});");
        if (fieldToSerializeList.Count > 0)
        {
            var linesToWrite = new List<string>();
            foreach (var field in fieldToSerializeList)
            {
                if (!field.ExistsInBaseType && field.Setter != null)
                {
                    linesToWrite.Add($"{DeserializerVarName}.put(\"{field.WireName}\", (n) -> {{ this.{field.Setter!.Name}(n.{GetDeserializationMethodName(field.Type, method)}); }});");
                }
            }
            linesToWrite.Sort();
            foreach (var line in linesToWrite)
            {
                writer.WriteLine(line);
            }
        }
        writer.WriteLine($"return {DeserializerVarName};");
    }
    private const string FactoryMethodName = "createFromDiscriminatorValue";
    private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement, false);
        WriteGeneratorOrExecutorMethodCall(codeElement, requestParams, parentClass, writer, $"final RequestInformation {RequestInfoVarName} = ", CodeMethodKind.RequestGenerator);

        var allTypesEnumerator = codeElement.ReturnType.AllTypes.GetEnumerator();
        if (!allTypesEnumerator.MoveNext()) throw new InvalidOperationException("AllTypes collection cannot be empty");
        var firstTypeDefinition = allTypesEnumerator.Current.TypeDefinition;

        var sendMethodName = GetSendRequestMethodName(codeElement.ReturnType.IsCollection, returnType, firstTypeDefinition is CodeEnum);
        var errorMappingVarName = "null";

        using var errorMappingsEnumerator = codeElement.ErrorMappings.GetEnumerator();
        if (!errorMappingsEnumerator.MoveNext()) throw new InvalidOperationException("ErrorMappings collection cannot be empty");
        errorMappingVarName = "errorMapping";
        writer.WriteLine($"final HashMap<String, ParsableFactory<? extends Parsable>> {errorMappingVarName} = new HashMap<String, ParsableFactory<? extends Parsable>>();");
        do
        {
            var errorMapping = errorMappingsEnumerator.Current;
            writer.WriteLine($"{errorMappingVarName}.put(\"{errorMapping.Key.ToUpperInvariant()}\", {errorMapping.Value.Name}::{FactoryMethodName});");
        } while (errorMappingsEnumerator.MoveNext());

        var factoryParameter = GetSendRequestFactoryParam(returnType, firstTypeDefinition is CodeEnum);
        var returnPrefix = codeElement.ReturnType.Name.Equals("void", StringComparison.OrdinalIgnoreCase) ? string.Empty : "return ";
        writer.WriteLine($"{returnPrefix}this.requestAdapter.{sendMethodName}({RequestInfoVarName}, {errorMappingVarName}, {factoryParameter});");
    }
    private string GetSendRequestMethodName(bool isCollection, string returnType, bool isEnum)
    {
        if (conventions.PrimitiveTypes.Contains(returnType))
            if (isCollection)
                return "sendPrimitiveCollection";
            else
                return "sendPrimitive";
        else if (isEnum)
            if (isCollection)
                return "sendEnumCollection";
            else
                return "sendEnum";
        else if (isCollection) return "sendCollection";
        return "send";
    }
    private string GetSendRequestFactoryParam(string returnType, bool isEnum)
    {
        if (conventions.PrimitiveTypes.Contains(returnType))
            return $"{returnType}.class";
        else if (isEnum)
            return $"{returnType}::forValue";
        else
            return $"{returnType}::{FactoryMethodName}";
    }

    private const string RequestInfoVarName = "requestInfo";
    private static void WriteGeneratorOrExecutorMethodCall(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass, LanguageWriter writer, string prefix, CodeMethodKind codeMethodKind)
    {
        if (codeElement.Kind is CodeMethodKind.RequestExecutor && codeElement.ReturnType.Name.Equals("void", StringComparison.OrdinalIgnoreCase) && prefix.Trim().Equals("return", StringComparison.OrdinalIgnoreCase))
            prefix = string.Empty;

        string? methodName = null;
        foreach (var method in parentClass.Methods)
        {
            if (method.IsOfKind(codeMethodKind) && method.HttpMethod == codeElement.HttpMethod)
            {
                methodName = method.Name.ToFirstCharacterLowerCase();
                break;
            }
        }

        var paramsList = new List<CodeParameter?> { requestParams.requestBody, requestParams.requestConfiguration };
        if (requestParams.requestContentType is not null)
            paramsList.Insert(1, requestParams.requestContentType);

        var requestInfoParameters = new List<string>();
        foreach (var param in paramsList)
        {
            if (param is CodeParameter codeParam)
            {
                requestInfoParameters.Add(codeParam.Name);
            }
        }

        var skipIndex = requestParams.requestBody == null ? 1 : 0;
        for (int i = skipIndex; i < paramsList.Count; i++)
        {
            if (paramsList[i] == null)
            {
                requestInfoParameters.Add("null");
            }
        }

        var paramsCall = string.Empty;
        if (requestInfoParameters.Count != 0)
        {
            paramsCall = requestInfoParameters[0];
            for (int i = 1; i < requestInfoParameters.Count; i++)
            {
                paramsCall += ", " + requestInfoParameters[i];
            }
        }

        writer.WriteLine($"{prefix}{methodName}({paramsCall});");
    }
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is not CodeProperty urlTemplateParamsProperty) throw new InvalidOperationException("url template params property cannot be null");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is not CodeProperty urlTemplateProperty) throw new InvalidOperationException("url template property cannot be null");

        var urlTemplateValue = codeElement.HasUrlTemplateOverride ? $"\"{codeElement.UrlTemplateOverride}\"" : GetPropertyCall(urlTemplateProperty, "\"\"");
        writer.WriteLine($"final RequestInformation {RequestInfoVarName} = new RequestInformation(HttpMethod.{codeElement.HttpMethod.ToString()?.ToUpperInvariant()}, {urlTemplateValue}, {GetPropertyCall(urlTemplateParamsProperty, "null")});");

        if (requestParams.requestConfiguration != null)
        {
            var queryStringName = requestParams.QueryParameters is null ? string.Empty : $", x -> x.{requestParams.QueryParameters.Name}";
            writer.WriteLine($"{RequestInfoVarName}.configure({requestParams.requestConfiguration.Name}, {requestParams.requestConfiguration.Type.Name}::new{queryStringName});");
        }

        if (codeElement.ShouldAddAcceptHeader)
            writer.WriteLine($"{RequestInfoVarName}.headers.tryAdd(\"Accept\", \"{codeElement.AcceptHeaderValue.SanitizeDoubleQuote()}\");");

        if (requestParams.requestBody != null &&
            currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
        {
            var toArrayPostfix = requestParams.requestBody.Type.IsCollection ? $".toArray(new {requestParams.requestBody.Type.Name}[0])" : string.Empty;
            var collectionPostfix = requestParams.requestBody.Type.IsCollection ? "Collection" : string.Empty;
            var sanitizedRequestBodyContentType = codeElement.RequestBodyContentType.SanitizeDoubleQuote();
            if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (requestParams.requestContentType is not null)
                    writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestParams.requestBody.Name}, {requestParams.requestContentType.Name});");
                else if (!string.IsNullOrEmpty(codeElement.RequestBodyContentType))
                    writer.WriteLine($"{RequestInfoVarName}.setStreamContent({requestParams.requestBody.Name}, \"{sanitizedRequestBodyContentType}\");");
            }
            else if (requestParams.requestBody.Type is CodeType bodyType && (bodyType.TypeDefinition is CodeClass || bodyType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)))
                writer.WriteLine($"{RequestInfoVarName}.setContentFromParsable({requestAdapterProperty.Name}, \"{sanitizedRequestBodyContentType}\", {requestParams.requestBody.Name}{toArrayPostfix});");
            else
                writer.WriteLine($"{RequestInfoVarName}.setContentFromScalar{collectionPostfix}({requestAdapterProperty.Name}, \"{sanitizedRequestBodyContentType}\", {requestParams.requestBody.Name}{toArrayPostfix});");
        }

        writer.WriteLine($"return {RequestInfoVarName};");
    }
    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"{property.Name}";
    private void WriteSerializerBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer, bool inherits)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteSerializerBodyForUnionModel(parentClass, method, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteSerializerBodyForIntersectionModel(parentClass, method, writer);
        else
            WriteSerializerBodyForInheritedModel(method, inherits, parentClass, writer);

        if (parentClass.GetPropertyOfKindFromAccessorOrDirect(CodePropertyKind.AdditionalData) is CodeProperty additionalDataProperty)
            writer.WriteLine($"writer.writeAdditionalData(this.get{additionalDataProperty.Name.ToFirstCharacterUpperCase()}());");
    }
    private void WriteSerializerBodyForUnionModel(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = new List<CodeProperty>();
        var properties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);

        foreach (var property in properties)
        {
            if (!property.ExistsInBaseType && property.Getter != null)
            {
                otherProps.Add(property);
            }
        }

        otherProps.Sort(CodePropertyTypeForwardComparer.Compare);

        foreach (var property in otherProps)
        {
            var otherProp = property;
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if (this.{otherProp.Getter!.Name}() != null) {{");
            WriteSerializationMethodCall(otherProp, method, writer, "null");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
        if (otherProps.Count != 0)
            writer.CloseBlock(decreaseIndent: false);
    }
    private void WriteSerializerBodyForIntersectionModel(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        var includeElse = false;
        var otherProps = new List<CodeProperty>();
        var complexProperties = new List<CodeProperty>();

        foreach (var prop in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom))
        {
            if (!prop.ExistsInBaseType && prop.Getter != null)
            {
                if (prop.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                {
                    otherProps.Add(prop);
                }
                else if (prop.Type is CodeType propType && propType.TypeDefinition is CodeClass && !prop.Type.IsCollection)
                {
                    complexProperties.Add(prop);
                }
            }
        }

        otherProps.Sort(CodePropertyTypeBackwardComparer.Compare);

        foreach (var otherProp in otherProps)
        {
            writer.StartBlock($"{(includeElse ? "} else " : string.Empty)}if (this.{otherProp.Getter!.Name}() != null) {{");
            WriteSerializationMethodCall(otherProp, method, writer, "null");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }

        if (complexProperties.Count != 0)
        {
            if (includeElse)
            {
                writer.WriteLine("} else {");
                writer.IncreaseIndent();
            }
            var propertiesNames = new List<string>();
            foreach (var prop in complexProperties)
            {
                if (prop.Getter != null)
                {
                    propertiesNames.Add($"this.{prop.Getter!.Name}()");
                }
            }
            propertiesNames.Sort();
            var propertiesNamesStr = string.Join(", ", propertiesNames);
            WriteSerializationMethodCall(complexProperties[0], method, writer, "null", propertiesNamesStr);
            if (includeElse)
            {
                writer.CloseBlock();
            }
        }
        else if (otherProps.Count != 0)
        {
            writer.CloseBlock(decreaseIndent: false);
        }
    }
    private void WriteSerializerBodyForInheritedModel(CodeMethod method, bool inherits, CodeClass parentClass, LanguageWriter writer)
    {
        if (inherits)
            writer.WriteLine("super.serialize(writer);");

        var properties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom);
        foreach (var property in properties)
        {
            if (!property.ExistsInBaseType && !property.ReadOnly)
            {
                WriteSerializationMethodCall(property, method, writer, $"\"{property.WireName}\"");
            }
        }
    }
    private void WriteSerializationMethodCall(CodeProperty otherProp, CodeMethod method, LanguageWriter writer, string serializationKey, string? dataToSerialize = default)
    {
        if (string.IsNullOrEmpty(dataToSerialize))
            dataToSerialize = $"this.{(otherProp.Getter?.Name is string gName && !string.IsNullOrEmpty(gName) ? gName : "get" + otherProp.Name)}()";
        writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type, method)}({serializationKey}, {dataToSerialize});");
    }
    private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
    private string GetFinalReturnType(CodeMethod code, string returnType)
    {
        if (code.ReturnType is CodeType { TypeDefinition: CodeEnum { Flags: true }, IsCollection: false })
            returnType = $"EnumSet<{returnType}>";
        var reType = returnType.Equals("void", StringComparison.OrdinalIgnoreCase) ? "void" : returnType;
        var collectionCorrectedReturnType = code.ReturnType.IsArray && code.IsOfKind(CodeMethodKind.RequestExecutor) ?
                                            $"Iterable<{returnType.StripArraySuffix()}>" :
                                            reType;
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
        var finalReturnType = isConstructor ? string.Empty : $"{collectionCorrectedReturnType}";
        return finalReturnType.Trim();
    }
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType)
    {
        var accessModifier = conventions.GetAccessModifier(code.Access);
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
        var methodName = isConstructor && code.Parent != null ? code.Parent.Name : code.Name;

        var parametersList = new List<string>();
        var sortedParameters = new List<CodeParameter>(code.Parameters);
        sortedParameters.Sort(parameterOrderComparer.Compare);
        foreach (var parameter in sortedParameters)
        {
            parametersList.Add(conventions.GetParameterSignature(parameter, code));
        }
        var parameters = string.Join(", ", parametersList);

        var reType = returnType.Equals("void", StringComparison.OrdinalIgnoreCase) ? "void" : returnType;
        var collectionCorrectedReturnType = code.ReturnType.IsArray && code.IsOfKind(CodeMethodKind.RequestExecutor) ?
                                            $"Iterable<{returnType.StripArraySuffix()}>" :
                                            reType;
        var staticModifier = code.IsStatic ? " static" : string.Empty;
        conventions.WriteDeprecatedAnnotation(code, writer);
        if (code.Kind is CodeMethodKind.ErrorMessageOverride)
        {
            writer.WriteLine($"@Override");
        }
        if (returnType.Length > 0)
            returnType = $" {returnType}";
        writer.WriteLine($"{accessModifier}{staticModifier}{returnType} {methodName}({parameters}) {{");
    }
    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer, string baseReturnType, string finalReturnType)
    {
        var returnVoid = baseReturnType.Equals("void", StringComparison.OrdinalIgnoreCase);
        var returnRemark = returnVoid ? string.Empty : conventions.GetReturnDocComment(finalReturnType);

        var paramComments = new List<string>();
        var sortedParameters = new List<CodeParameter>(code.Parameters);
        sortedParameters.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));

        foreach (var param in sortedParameters)
        {
            if (param.Documentation.DescriptionAvailable)
            {
                var paramComment = $"@param {param.Name} {param.Documentation.GetDescription(y => conventions.GetTypeReferenceForDocComment(y, code), normalizationFunc: JavaConventionService.RemoveInvalidDescriptionCharacters)}";
                paramComments.Add(paramComment);
            }
        }

        if (!string.IsNullOrEmpty(returnRemark))
        {
            paramComments.Add(returnRemark);
        }

        var exceptionDocRemarks = GetExceptionDocRemarks(code);
        foreach (var remark in exceptionDocRemarks)
        {
            paramComments.Add(remark);
        }

        conventions.WriteLongDescription(code, writer, paramComments);

        if (!returnVoid)
        {
            writer.WriteLine(code.ReturnType.IsNullable ? "@jakarta.annotation.Nullable" : "@jakarta.annotation.Nonnull");
        }
    }
    private IEnumerable<string> GetExceptionDocRemarks(CodeMethod code)
    {
        if (code.Kind is not CodeMethodKind.RequestExecutor)
            yield break;

        foreach (var errorMapping in code.ErrorMappings)
        {
            var statusCode = errorMapping.Key.ToUpperInvariant() switch
            {
                "XXX" => "4XX or 5XX",
                _ => errorMapping.Key,
            };
            var errorTypeString = conventions.GetTypeString(errorMapping.Value, code);
            yield return $"@throws {errorTypeString} When receiving a {statusCode} status code";
        }
    }
    private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"getCollectionOfPrimitiveValues({propertyType.ToFirstCharacterUpperCase()}.class)";
                else if (currentType.TypeDefinition is CodeEnum enumType)
                    return $"getCollectionOfEnumValues({enumType.Name}::forValue)";
                else
                    return $"getCollectionOfObjectValues({propertyType.ToFirstCharacterUpperCase()}::{FactoryMethodName})";
            if (currentType.TypeDefinition is CodeEnum currentEnum)
            {
                var returnType = propertyType.ToFirstCharacterUpperCase();
                return $"getEnum{(currentEnum.Flags ? "Set" : string.Empty)}Value({returnType}::forValue)";
            }

        }
        return propertyType switch
        {
            "byte[]" => "getByteArrayValue()",
            _ when conventions.PrimitiveTypes.Contains(propertyType) => $"get{propertyType}Value()",
            _ => $"getObjectValue({propertyType.ToFirstCharacterUpperCase()}::{FactoryMethodName})",
        };
    }
    private string GetSerializationMethodName(CodeTypeBase propType, CodeMethod method)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.GetTypeString(propType, method, false);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return "writeCollectionOfPrimitiveValues";
                else if (currentType.TypeDefinition is CodeEnum)
                    return "writeCollectionOfEnumValues";
                else
                    return "writeCollectionOfObjectValues";
            if (currentType.TypeDefinition is CodeEnum currentEnum)
                return $"writeEnum{(currentEnum.Flags ? "Set" : string.Empty)}Value";
        }
        return propertyType switch
        {
            "byte[]" => "writeByteArrayValue",
            _ when conventions.PrimitiveTypes.Contains(propertyType) => $"write{propertyType}Value",
            _ => "writeObjectValue",
        };
    }
}
