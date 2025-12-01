using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python;

public class CodeMethodWriter : BaseElementWriter<CodeMethod, PythonConventionService>
{
    private readonly CodeUsingWriter _codeUsingWriter;
    public CodeMethodWriter(PythonConventionService conventionService, string clientNamespaceName, bool usesBackingStore) : base(conventionService)
    {
        _codeUsingWriter = new(clientNamespaceName);
        _usesBackingStore = usesBackingStore;
    }
    private readonly bool _usesBackingStore;
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("the parent of a method should be a class");

        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement, true, writer);
        var returnTypeIsEnum = codeElement.ReturnType is CodeType { TypeDefinition: CodeEnum };
        var isVoid = NoneKeyword.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        if (parentClass.IsOfKind(CodeClassKind.Model) && (codeElement.IsOfKind(CodeMethodKind.Setter) || codeElement.IsOfKind(CodeMethodKind.Getter) || codeElement.IsOfKind(CodeMethodKind.Constructor)))
        {
            writer.IncreaseIndent();
        }
        else
        {
            WriteMethodPrototype(codeElement, writer, returnType, isVoid);
            writer.IncreaseIndent();
            WriteMethodDocumentation(codeElement, writer, returnType, isVoid);
        }
        var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
        var requestBodyParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestBody);
        var requestConfigParam = codeElement.Parameters.OfKind(CodeParameterKind.RequestConfiguration);
        var requestContentType = codeElement.Parameters.OfKind(CodeParameterKind.RequestBodyContentType);
        var requestParams = new RequestParams(requestBodyParam, requestConfigParam, requestContentType);
        if (!codeElement.IsOfKind(CodeMethodKind.Setter) &&
        !(codeElement.IsOfKind(CodeMethodKind.Constructor) && parentClass.IsOfKind(CodeClassKind.RequestBuilder)))
            foreach (var parameter in codeElement.Parameters.Where(static x => !x.Optional).OrderBy(static x => x.Name))
            {
                var parameterName = parameter.Name;
                writer.StartBlock($"if {parameterName} is None:");
                writer.WriteLine($"raise TypeError(\"{parameterName} cannot be null.\")");
                writer.DecreaseIndent();
            }
        switch (codeElement.Kind)
        {
            case CodeMethodKind.ClientConstructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                WriteApiConstructorBody(parentClass, codeElement, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.RawUrlBuilder:
                WriteRawUrlBuilderBody(parentClass, codeElement, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.Constructor:
                WriteConstructorBody(parentClass, codeElement, writer, inherits);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.IndexerBackwardCompatibility:
                WriteIndexerBody(codeElement, parentClass, returnType, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.Deserializer:
                WriteDeserializerBody(codeElement, parentClass, writer, inherits);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.Serializer:
                WriteSerializerBody(inherits, parentClass, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.RequestGenerator:
                WriteRequestGeneratorBody(codeElement, requestParams, parentClass, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.RequestExecutor:
                WriteRequestExecutorBody(codeElement, requestParams, parentClass, isVoid, returnType, writer, returnTypeIsEnum);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.Getter:
                WriteGetterBody(codeElement, writer, parentClass);
                break;
            case CodeMethodKind.Setter:
                WriteSetterBody(codeElement, writer, parentClass);
                break;
            case CodeMethodKind.RequestBuilderWithParameters:
                WriteRequestBuilderWithParametersBody(codeElement, parentClass, returnType, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.QueryParametersMapper:
                WriteQueryParametersMapper(codeElement, parentClass, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.Factory:
                WriteFactoryMethodBody(codeElement, parentClass, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodeMethodKind.ComposedTypeMarker:
                throw new InvalidOperationException("ComposedTypeMarker is not required as interface is explicitly implemented.");
            case CodeMethodKind.RawUrlConstructor:
                throw new InvalidOperationException("RawUrlConstructor is not supported in python");
            case CodeMethodKind.RequestBuilderBackwardCompatibility:
                throw new InvalidOperationException("RequestBuilderBackwardCompatibility is not supported as the request builders are implemented by properties.");
            default:
                WriteDefaultMethodBody(codeElement, writer, returnType);
                writer.CloseBlock(string.Empty);
                break;
        }
    }
    private void WriteRawUrlBuilderBody(CodeClass parentClass, CodeMethod codeElement, LanguageWriter writer)
    {
        var rawUrlParameter = codeElement.Parameters.OfKind(CodeParameterKind.RawUrl) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RawUrl parameter");
        var requestAdapterProperty = parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) ?? throw new InvalidOperationException("RawUrlBuilder method should have a RequestAdapter property");
        writer.WriteLine($"return {parentClass.Name}(self.{requestAdapterProperty.Name}, {rawUrlParameter.Name})");
    }
    private const string DiscriminatorMappingVarName = "mapping_value";

    private static readonly CodePropertyTypeComparer CodePropertyTypeForwardComparer = new();
    private static readonly CodePropertyTypeComparer CodePropertyTypeBackwardComparer = new(true);
    private void WriteFactoryMethodBodyForInheritedModel(CodeClass parentClass, LanguageWriter writer)
    {
        foreach (var mappedType in parentClass.DiscriminatorInformation.DiscriminatorMappings.OrderBy(static x => x.Key))
        {
            writer.StartBlock($"if {DiscriminatorMappingVarName} and {DiscriminatorMappingVarName}.casefold() == \"{mappedType.Key}\".casefold():");
            var mappedTypeName = mappedType.Value.AllTypes.First().Name;
            _codeUsingWriter.WriteDeferredImport(parentClass, mappedTypeName, writer);
            writer.WriteLine($"return {mappedTypeName}()");
            writer.DecreaseIndent();
        }
        writer.WriteLine($"return {parentClass.Name}()");
    }
    private const string ResultVarName = "result";
    private void WriteFactoryMethodBodyForUnionModel(CodeMethod codeElement, CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        writer.WriteLine($"{ResultVarName} = {parentClass.Name}()");
        var includeElse = false;
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                            .ThenBy(static x => x.Name))
        {
            if (property.Type is CodeType propertyType)
                if (propertyType.TypeDefinition is CodeClass && !propertyType.IsCollection)
                {
                    var mappedType = parentClass.DiscriminatorInformation.DiscriminatorMappings.FirstOrDefault(x => x.Value.Name.Equals(propertyType.Name, StringComparison.OrdinalIgnoreCase));
                    writer.StartBlock($"{(includeElse ? "el" : string.Empty)}if {DiscriminatorMappingVarName} and {DiscriminatorMappingVarName}.casefold() == \"{mappedType.Key}\".casefold():");
                    _codeUsingWriter.WriteDeferredImport(parentClass, propertyType.Name, writer);
                    writer.WriteLine($"{ResultVarName}.{property.Name} = {propertyType.Name}()");
                    writer.DecreaseIndent();
                }
                else if (propertyType.TypeDefinition is CodeClass && propertyType.IsCollection || propertyType.TypeDefinition is null || propertyType.TypeDefinition is CodeEnum)
                {
                    var valueVarName = $"{property.Name}_value";
                    writer.StartBlock($"{(includeElse ? "el" : string.Empty)}if {valueVarName} := {parseNodeParameter.Name}.{GetDeserializationMethodName(propertyType, codeElement, parentClass)}:");
                    writer.WriteLine($"{ResultVarName}.{property.Name} = {valueVarName}");
                    writer.DecreaseIndent();
                }
            if (!includeElse)
                includeElse = true;
        }
        writer.WriteLine($"return {ResultVarName}");
    }
    private void WriteFactoryMethodBodyForIntersectionModel(CodeMethod codeElement, CodeClass parentClass, CodeParameter parseNodeParameter, LanguageWriter writer)
    {
        writer.WriteLine($"{ResultVarName} = {parentClass.Name}()");
        var includeElse = false;
        foreach (var property in parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                            .OrderBy(static x => x, CodePropertyTypeBackwardComparer)
                                            .ThenBy(static x => x.Name))
        {
            if (property.Type is CodeType propertyType)
            {
                var valueVarName = $"{property.Name}_value";
                writer.StartBlock($"{(includeElse ? "el" : string.Empty)}if {valueVarName} := {parseNodeParameter.Name}.{GetDeserializationMethodName(propertyType, codeElement, parentClass)}:");
                writer.WriteLine($"{ResultVarName}.{property.Name} = {valueVarName}");
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
            {
                writer.StartBlock("else:");
            }
            foreach (var property in complexProperties)
            {
                _codeUsingWriter.WriteDeferredImport(parentClass, property.Item2.Name, writer);
                writer.WriteLine($"{ResultVarName}.{property.Item1.Name} = {property.Item2.Name}()");
            }
            if (includeElse)
            {
                writer.DecreaseIndent();
            }
        }
        writer.WriteLine($"return {ResultVarName}");
    }
    private void WriteFactoryMethodBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parseNodeParameter = codeElement.Parameters.OfKind(CodeParameterKind.ParseNode) ?? throw new InvalidOperationException("Factory method should have a ParseNode parameter");

        if (parentClass.DiscriminatorInformation.ShouldWriteParseNodeCheck && !parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
        {
            writer.StartBlock("try:");
            writer.WriteLine($"child_node = {parseNodeParameter.Name}.get_child_node(\"{parentClass.DiscriminatorInformation.DiscriminatorPropertyName}\")");
            writer.WriteLine($"{DiscriminatorMappingVarName} = child_node.get_str_value() if child_node else {NoneKeyword}");
            writer.DecreaseIndent();
            writer.StartBlock("except AttributeError:");
            writer.WriteLine($"{DiscriminatorMappingVarName} = {NoneKeyword}");
            writer.DecreaseIndent();
        }
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForInheritedType)
            WriteFactoryMethodBodyForInheritedModel(parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteFactoryMethodBodyForUnionModel(codeElement, parentClass, parseNodeParameter, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteFactoryMethodBodyForIntersectionModel(codeElement, parentClass, parseNodeParameter, writer);
        else
            writer.WriteLine($"return {parentClass.Name}()");
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        _codeUsingWriter.WriteDeferredImport(parentClass, codeElement.ReturnType.Name, writer);
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            codeElement.OriginalIndexer != null)
            conventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"self.{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexParameter.Type, codeElement.OriginalIndexer.IndexParameter.SerializationName, codeElement.OriginalIndexer.IndexParameter.Name));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, conventions.TempDictionaryVarName);
    }
    private void WriteRequestBuilderWithParametersBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        _codeUsingWriter.WriteDeferredImport(parentClass, codeElement.ReturnType.Name, writer);
        var codePathParameters = codeElement.Parameters
                                                    .Where(x => x.IsOfKind(CodeParameterKind.Path));
        conventions.AddRequestBuilderBody(parentClass, returnType, writer, pathParameters: codePathParameters);
    }
    private static void WriteApiConstructorBody(CodeClass parentClass, CodeMethod method, LanguageWriter writer)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is not CodeProperty requestAdapterProperty) return;
        var backingStoreParameter = method.Parameters.OfKind(CodeParameterKind.BackingStore);
        var requestAdapterPropertyName = requestAdapterProperty.Name;
        var pathParametersProperty = parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters);
        WriteSerializationRegistration(method.SerializerModules, writer, "register_default_serializer");
        WriteSerializationRegistration(method.DeserializerModules, writer, "register_default_deserializer");
        if (!string.IsNullOrEmpty(method.BaseUrl))
        {
            writer.StartBlock($"if not self.{requestAdapterPropertyName}.base_url:");
            writer.WriteLine($"self.{requestAdapterPropertyName}.base_url = \"{method.BaseUrl}\"");
            writer.DecreaseIndent();
            if (pathParametersProperty != null)
                writer.WriteLine($"self.{pathParametersProperty.Name}[\"base_url\"] = self.{requestAdapterPropertyName}.base_url");
        }
        if (backingStoreParameter != null)
            writer.WriteLine($"self.{requestAdapterPropertyName}.enable_backing_store({backingStoreParameter.Name})");
    }
    private static void WriteQueryParametersMapper(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        var parameter = codeElement.Parameters.FirstOrDefault(x => x.IsOfKind(CodeParameterKind.QueryParametersMapperParameter));
        if (parameter == null) throw new InvalidOperationException("QueryParametersMapper should have a parameter of type QueryParametersMapper");
        var parameterName = parameter.Name;
        var escapedProperties = parentClass.Properties.Where(x => x.IsOfKind(CodePropertyKind.QueryParameter) && x.IsNameEscaped);
        var unescapedProperties = parentClass.Properties.Where(x => x.IsOfKind(CodePropertyKind.QueryParameter) && !x.IsNameEscaped);
        foreach (var escapedProperty in escapedProperties)
        {
            writer.StartBlock($"if {parameterName} == \"{escapedProperty.Name}\":");
            writer.WriteLine($"return \"{escapedProperty.SerializationName}\"");
            writer.DecreaseIndent();
        }
        foreach (var unescapedProperty in unescapedProperties.Select(x => x.Name))
        {
            writer.StartBlock($"if {parameterName} == \"{unescapedProperty}\":");
            writer.WriteLine($"return \"{unescapedProperty}\"");
            writer.DecreaseIndent();
        }
        writer.WriteLine($"return {parameterName}");
    }
    private static void WriteSerializationRegistration(HashSet<string> serializationModules, LanguageWriter writer, string methodName)
    {
        if (serializationModules != null)
            foreach (var module in serializationModules)
                writer.WriteLine($"{methodName}({module})");
    }
    private CodePropertyKind[]? _DirectAccessProperties;
    private CodePropertyKind[] DirectAccessProperties
    {
        get
        {
            if (_DirectAccessProperties == null)
            {
                var directAccessProperties = new List<CodePropertyKind> {
                CodePropertyKind.BackingStore,
                CodePropertyKind.RequestBuilder,
                CodePropertyKind.UrlTemplate,
                CodePropertyKind.PathParameters
            };
                if (!_usesBackingStore)
                {
                    directAccessProperties.Add(CodePropertyKind.AdditionalData);
                }
                _DirectAccessProperties = directAccessProperties.ToArray();
            }
            return _DirectAccessProperties;
        }
    }
    private CodePropertyKind[]? _SetterAccessProperties;
    private CodePropertyKind[] SetterAccessProperties
    {
        get
        {
            _SetterAccessProperties ??= new CodePropertyKind[] {
                    CodePropertyKind.AdditionalData, //additional data and custom properties need to use the accessors in case of backing store use
                    CodePropertyKind.Custom
                }.Except(DirectAccessProperties)
                .ToArray();
            return _SetterAccessProperties;
        }
    }
    private void WriteConstructorBody(CodeClass parentClass, CodeMethod currentMethod, LanguageWriter writer, bool inherits)
    {
        if (inherits && !parentClass.IsOfKind(CodeClassKind.Model))
        {
            if (parentClass.IsOfKind(CodeClassKind.RequestBuilder) &&
            currentMethod.Parameters.OfKind(CodeParameterKind.RequestAdapter) is CodeParameter requestAdapterParameter &&
            parentClass.Properties.FirstOrDefaultOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty)
            {
                if (currentMethod.Parameters.OfKind(CodeParameterKind.PathParameters) is CodeParameter pathParametersParameter)
                {
                    var pathParameters = currentMethod.Parameters
                                                                .Where(static x => x.IsOfKind(CodeParameterKind.Path))
                                                                .Select(static x => (string.IsNullOrEmpty(x.SerializationName) ? x.Name : x.SerializationName, x.Name))
                                                                .ToArray();
                    if (pathParameters.Length != 0)
                    {
                        writer.StartBlock($"if isinstance({pathParametersParameter.Name}, dict):");
                        foreach (var parameter in pathParameters)
                        {
                            var (name, identName) = parameter;
                            writer.WriteLine($"{pathParametersParameter.Name}['{name}'] = {identName}");
                        }
                        writer.DecreaseIndent();
                    }
                    writer.WriteLine($"super().__init__({requestAdapterParameter.Name}, {urlTemplateProperty.DefaultValue ?? ""}, {pathParametersParameter.Name})");
                }
                else
                    writer.WriteLine($"super().__init__({requestAdapterParameter.Name}, {urlTemplateProperty.DefaultValue ?? ""}, {NoneKeyword})");
            }
            else
                writer.WriteLine("super().__init__()");
        }
        if (parentClass.IsOfKind(CodeClassKind.Model))
        {
            writer.DecreaseIndent();
        }

        if (!(parentClass.IsOfKind(CodeClassKind.RequestBuilder) && currentMethod.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor)))
        {
            WriteDirectAccessProperties(parentClass, writer);
            WriteSetterAccessProperties(parentClass, writer);
            WriteSetterAccessPropertiesWithoutDefaults(parentClass, writer);
        }

        if (parentClass.IsOfKind(CodeClassKind.Model))
        {
            writer.IncreaseIndent();
        }
    }
    private void WriteDirectAccessProperties(CodeClass parentClass, LanguageWriter writer)
    {
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(DirectAccessProperties)
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue) && !NoneKeyword.Equals(x.DefaultValue, StringComparison.Ordinal))
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name))
        {
            var returnType = conventions.GetTypeString(propWithDefault.Type, propWithDefault, true, writer);
            var defaultValue = propWithDefault.DefaultValue;
            switch (propWithDefault.Type)
            {
                case CodeType { TypeDefinition: CodeEnum enumDefinition }:
                    _codeUsingWriter.WriteDeferredImport(parentClass, enumDefinition.Name, writer);
                    defaultValue = $"{enumDefinition.Name}({defaultValue})";
                    break;
                case CodeType propType when propType.Name.Equals("boolean", StringComparison.OrdinalIgnoreCase):
                    defaultValue = defaultValue.TrimQuotes().ToFirstCharacterUpperCase();// python booleans start in uppercase
                    break;
            }
            conventions.WriteInLineDescription(propWithDefault, writer);
            if (parentClass.IsOfKind(CodeClassKind.Model))
            {
                writer.WriteLine($"{propWithDefault.Name}: {(propWithDefault.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(propWithDefault.Type.IsNullable ? "]" : string.Empty)} = {defaultValue}");
                writer.WriteLine();
            }
            else
            {
                writer.WriteLine($"self.{conventions.GetAccessModifier(propWithDefault.Access)}{propWithDefault.NamePrefix}{propWithDefault.Name}: {(propWithDefault.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(propWithDefault.Type.IsNullable ? "]" : string.Empty)} = {defaultValue}");
                writer.WriteLine();
            }
        }
    }
    private void WriteSetterAccessProperties(CodeClass parentClass, LanguageWriter writer)
    {
        foreach (var propWithDefault in parentClass.GetPropertiesOfKind(SetterAccessProperties)
                                        .Where(static x => !string.IsNullOrEmpty(x.DefaultValue) && !NoneKeyword.Equals(x.DefaultValue, StringComparison.Ordinal))
                                        // do not apply the default value if the type is composed as the default value may not necessarily which type to use
                                        .Where(static x => x.Type is not CodeType propType || propType.TypeDefinition is not CodeClass propertyClass || propertyClass.OriginalComposedType is null)
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name))
        {
            var defaultValue = propWithDefault.DefaultValue;
            switch (propWithDefault.Type)
            {
                case CodeType { TypeDefinition: CodeEnum enumDefinition }:
                    _codeUsingWriter.WriteDeferredImport(parentClass, enumDefinition.Name, writer);
                    defaultValue = $"{enumDefinition.Name}({defaultValue})";
                    break;
                case CodeType propType when propType.Name.Equals("boolean", StringComparison.OrdinalIgnoreCase):
                    defaultValue = defaultValue.TrimQuotes().ToFirstCharacterUpperCase();// python booleans start in uppercase
                    break;
            }
            var returnType = conventions.GetTypeString(propWithDefault.Type, propWithDefault, true, writer);
            conventions.WriteInLineDescription(propWithDefault, writer);
            var setterString = $"{propWithDefault.Name}: {(propWithDefault.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(propWithDefault.Type.IsNullable ? "]" : string.Empty)} = {defaultValue}";
            if (parentClass.IsOfKind(CodeClassKind.Model))
            {
                writer.WriteLine($"{setterString}");
            }
            else
                writer.WriteLine($"self.{setterString}");
        }
    }
    private const string NoneKeyword = "None";
    private void WriteSetterAccessPropertiesWithoutDefaults(CodeClass parentClass, LanguageWriter writer)
    {
        foreach (var propWithoutDefault in parentClass.GetPropertiesOfKind(SetterAccessProperties)
                                        .Where(static x => string.IsNullOrEmpty(x.DefaultValue) || NoneKeyword.Equals(x.DefaultValue, StringComparison.Ordinal))
                                        .OrderByDescending(static x => x.Kind)
                                        .ThenBy(static x => x.Name))
        {
            var returnType = conventions.GetTypeString(propWithoutDefault.Type, propWithoutDefault, true, writer);
            conventions.WriteInLineDescription(propWithoutDefault, writer);
            if (parentClass.IsOfKind(CodeClassKind.Model))
                writer.WriteLine($"{propWithoutDefault.Name}: {(propWithoutDefault.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(propWithoutDefault.Type.IsNullable ? "]" : string.Empty)} = {NoneKeyword}");
            else
                writer.WriteLine($"self.{conventions.GetAccessModifier(propWithoutDefault.Access)}{propWithoutDefault.NamePrefix}{propWithoutDefault.Name}: {(propWithoutDefault.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(propWithoutDefault.Type.IsNullable ? "]" : string.Empty)} = {NoneKeyword}");
        }
    }
    private static void WriteSetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass)
    {
        if (!parentClass.IsOfKind(CodeClassKind.Model))
        {
            var backingStore = parentClass.GetBackingStoreProperty();
            if (backingStore == null)
                writer.WriteLine($"self.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name} = value");
            else
                writer.WriteLine($"self.{backingStore.NamePrefix}{backingStore.Name}[\"{codeElement.AccessedProperty?.Name}\"] = value");
            writer.CloseBlock(string.Empty);
        }
        else
            writer.DecreaseIndent();
    }
    private void WriteGetterBody(CodeMethod codeElement, LanguageWriter writer, CodeClass parentClass)
    {
        if (!parentClass.IsOfKind(CodeClassKind.Model))
        {
            var backingStore = parentClass.GetBackingStoreProperty();
            if (backingStore == null)
                writer.WriteLine($"return self.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name}");
            else
                if (!(codeElement.AccessedProperty?.Type?.IsNullable ?? true) &&
                    !(codeElement.AccessedProperty?.ReadOnly ?? true) &&
                    !string.IsNullOrEmpty(codeElement.AccessedProperty?.DefaultValue))
            {
                writer.WriteLines($"value: {conventions.GetTypeString(codeElement.AccessedProperty.Type, codeElement, true, writer)} = self.{backingStore.NamePrefix}{backingStore.Name}.get(\"{codeElement.AccessedProperty.Name}\")",
                    "if not value:");
                writer.IncreaseIndent();
                writer.WriteLines($"value = {codeElement.AccessedProperty.DefaultValue}",
                    $"self.{codeElement.AccessedProperty?.NamePrefix}{codeElement.AccessedProperty?.Name} = value");
                writer.DecreaseIndent();
                writer.WriteLines("return value");
            }
            else
                writer.WriteLine($"return self.{backingStore.NamePrefix}{backingStore.Name}.get(\"{codeElement.AccessedProperty?.Name}\")");
            writer.CloseBlock(string.Empty);
        }
        else
            writer.DecreaseIndent();
    }
    private static void WriteDefaultMethodBody(CodeMethod codeElement, LanguageWriter writer, string returnType)
    {
        var promisePrefix = codeElement.IsAsync ? "await " : string.Empty;
        writer.WriteLine($"return {promisePrefix}{returnType}()");
    }
    private const string DefaultDeserializerValue = "{}";
    private void WriteDeserializerBody(CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer, bool inherits)
    {
        _codeUsingWriter.WriteInternalImports(parentClass, writer);
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteDeserializerBodyForUnionModel(codeElement, parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteDeserializerBodyForIntersectionModel(parentClass, writer);
        else
            WriteDeserializerBodyForInheritedModel(inherits, codeElement, parentClass, writer);
    }
    private void WriteDeserializerBodyForUnionModel(CodeMethod method, CodeClass parentClass, LanguageWriter writer)
    {
        foreach (var otherPropName in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType)
                                        .Where(static x => x.Type is CodeType propertyType && !propertyType.IsCollection && propertyType.TypeDefinition is CodeClass)
                                        .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                        .ThenBy(static x => x.Name)
                                        .Select(static x => x.Name))
        {
            writer.StartBlock($"if self.{otherPropName}:");
            writer.WriteLine($"return self.{otherPropName}.{method.Name}()");
            writer.DecreaseIndent();
        }
        writer.WriteLine($"return {DefaultDeserializerValue}");
    }
    private void WriteDeserializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var complexProperties = parentClass.GetPropertiesOfKind(CodePropertyKind.Custom)
                                            .Where(static x => x.Type is CodeType propType && propType.TypeDefinition is CodeClass && !x.Type.IsCollection)
                                            .ToArray();
        if (complexProperties.Length != 0)
        {
            var propertiesNames = complexProperties
                                .Select(static x => x.Name)
                                .OrderBy(static x => x)
                                .ToArray();
            var propertiesNamesAsConditions = propertiesNames
                                .Select(static x => $"self.{x}")
                                .Aggregate(static (x, y) => $"{x} or {y}");
            writer.StartBlock($"if {propertiesNamesAsConditions}:");
            var propertiesNamesAsArgument = propertiesNames
                                .Select(static x => $"self.{x}")
                                .Aggregate(static (x, y) => $"{x}, {y}");
            writer.WriteLine($"return ParseNodeHelper.merge_deserializers_for_intersection_wrapper({propertiesNamesAsArgument})");
            writer.DecreaseIndent();
        }
        writer.WriteLine($"return {DefaultDeserializerValue}");
    }
    private void WriteDeserializerBodyForInheritedModel(bool inherits, CodeMethod codeElement, CodeClass parentClass, LanguageWriter writer)
    {
        _codeUsingWriter.WriteInternalImports(parentClass, writer);
        writer.StartBlock($"fields: dict[str, Callable[[Any], {NoneKeyword}]] = {{");
        foreach (var otherProp in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType)
                                        .OrderBy(static x => x.Name))
        {
            writer.WriteLine($"\"{otherProp.WireName}\": lambda n : setattr(self, '{otherProp.Name}', n.{GetDeserializationMethodName(otherProp.Type, codeElement, parentClass)}),");
        }
        writer.CloseBlock();
        if (inherits)
        {
            writer.WriteLine($"super_fields = super().{codeElement.Name}()");
            writer.WriteLine("fields.update(super_fields)");
        }
        writer.WriteLine("return fields");
    }
    private void WriteRequestExecutorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass parentClass,
        bool isVoid, string returnType, LanguageWriter writer, bool isEnum)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        var generatorMethodName = parentClass
                                            .Methods
                                            .FirstOrDefault(x => x.IsOfKind(CodeMethodKind.RequestGenerator) && x.HttpMethod == codeElement.HttpMethod)
                                            ?.Name;
        writer.WriteLine($"request_info = self.{generatorMethodName}(");
        var requestInfoParameters = new CodeParameter?[] { requestParams.requestBody, requestParams.requestContentType, requestParams.requestConfiguration }
                                        .OfType<CodeParameter>()
                                        .Select(static x => x.Name)
                                        .ToArray();
        if (requestInfoParameters.Length != 0)
        {
            writer.IncreaseIndent();
            writer.WriteLine(requestInfoParameters.Aggregate(static (x, y) => $"{x}, {y}"));
            writer.DecreaseIndent();
        }
        writer.WriteLine(")");
        var isStream = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        var returnTypeWithoutCollectionSymbol = GetReturnTypeWithoutCollectionSymbol(codeElement, returnType);
        var genericTypeForSendMethod = GetSendRequestMethodName(isVoid, isStream, codeElement.ReturnType.IsCollection, returnTypeWithoutCollectionSymbol, isEnum);
        var newFactoryParameter = GetTypeFactory(isVoid, isStream, isEnum, returnTypeWithoutCollectionSymbol, codeElement.ReturnType.IsCollection);
        var errorMappingVarName = NoneKeyword;
        if (codeElement.ErrorMappings.Any())
        {
            _codeUsingWriter.WriteInternalErrorMappingImports(parentClass, writer);
            errorMappingVarName = "error_mapping";
            writer.StartBlock($"{errorMappingVarName}: dict[str, type[ParsableFactory]] = {{");
            foreach (var errorMapping in codeElement.ErrorMappings)
            {
                writer.WriteLine($"\"{errorMapping.Key.ToUpperInvariant()}\": {errorMapping.Value.Name},");
            }
            writer.CloseBlock();
        }
        writer.StartBlock("if not self.request_adapter:");
        writer.WriteLine("raise Exception(\"Http core is null\") ");
        writer.DecreaseIndent();
        _codeUsingWriter.WriteDeferredImport(parentClass, codeElement.ReturnType.Name, writer);
        writer.WriteLine($"return await self.request_adapter.{genericTypeForSendMethod}(request_info,{newFactoryParameter} {errorMappingVarName})");
    }
    private string GetReturnTypeWithoutCollectionSymbol(CodeMethod codeElement, string fullTypeName)
    {
        if (!codeElement.ReturnType.IsCollection) return fullTypeName;
        if (codeElement.ReturnType.Clone() is CodeTypeBase clone)
        {
            clone.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.None;
            return conventions.GetTypeString(clone, codeElement);
        }
        return string.Empty;
    }
    private const string RequestInfoVarName = "request_info";
    private void WriteRequestGeneratorBody(CodeMethod codeElement, RequestParams requestParams, CodeClass currentClass, LanguageWriter writer)
    {
        if (codeElement.HttpMethod == null) throw new InvalidOperationException("http method cannot be null");

        if (currentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is not CodeProperty urlTemplateParamsProperty) throw new InvalidOperationException("path parameters cannot be null");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is not CodeProperty urlTemplateProperty) throw new InvalidOperationException("url template cannot be null");
        var urlTemplateValue = codeElement.HasUrlTemplateOverride ? $"'{codeElement.UrlTemplateOverride}'" : GetPropertyCall(urlTemplateProperty, "''");
        writer.WriteLine($"{RequestInfoVarName} = RequestInformation(Method.{codeElement.HttpMethod.Value.ToString().ToUpperInvariant()}, {urlTemplateValue}, {GetPropertyCall(urlTemplateParamsProperty, "''")})");
        if (requestParams.requestConfiguration != null)
            writer.WriteLine($"{RequestInfoVarName}.configure({requestParams.requestConfiguration.Name})");
        if (codeElement.ShouldAddAcceptHeader)
            writer.WriteLine($"{RequestInfoVarName}.headers.try_add(\"Accept\", \"{codeElement.AcceptHeaderValue.SanitizeDoubleQuote()}\")");
        if (currentClass.GetPropertyOfKind(CodePropertyKind.RequestAdapter) is CodeProperty requestAdapterProperty)
            UpdateRequestInformationFromRequestBody(codeElement, requestParams, requestAdapterProperty, writer);
        writer.WriteLine($"return {RequestInfoVarName}");
    }
    private static string GetPropertyCall(CodeProperty property, string defaultValue) => property == null ? defaultValue : $"self.{property.Name}";
    private void WriteSerializerBody(bool inherits, CodeClass parentClass, LanguageWriter writer)
    {
        if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForUnionType)
            WriteSerializerBodyForUnionModel(parentClass, writer);
        else if (parentClass.DiscriminatorInformation.ShouldWriteDiscriminatorForIntersectionType)
            WriteSerializerBodyForIntersectionModel(parentClass, writer);
        else
            WriteSerializerBodyForInheritedModel(inherits, parentClass, writer);

        if (parentClass.GetPropertyOfKind(CodePropertyKind.AdditionalData) is CodeProperty additionalDataProperty)
            writer.WriteLine($"writer.write_additional_data_value(self.{additionalDataProperty.Name})");
    }
    private void WriteSerializerBodyForInheritedModel(bool inherits, CodeClass parentClass, LanguageWriter writer)
    {
        if (inherits)
            writer.WriteLine("super().serialize(writer)");
        foreach (var otherProp in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType && !x.ReadOnly)
                                        .OrderBy(static x => x.Name))
        {
            var serializationMethodName = GetSerializationMethodName(otherProp.Type);
            writer.WriteLine($"writer.{serializationMethodName}(\"{otherProp.WireName}\", self.{otherProp.Name})");
        }
    }
    private void WriteSerializerBodyForUnionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        foreach (var otherProp in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType)
                                        .OrderBy(static x => x, CodePropertyTypeForwardComparer)
                                        .ThenBy(static x => x.Name))
        {
            writer.StartBlock($"{(includeElse ? "el" : string.Empty)}if self.{otherProp.Name}:");
            writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}({NoneKeyword}, self.{otherProp.Name})");
            writer.DecreaseIndent();
            if (!includeElse)
                includeElse = true;
        }
    }
    private void WriteSerializerBodyForIntersectionModel(CodeClass parentClass, LanguageWriter writer)
    {
        var includeElse = false;
        foreach (var otherProp in parentClass
                                        .GetPropertiesOfKind(CodePropertyKind.Custom)
                                        .Where(static x => !x.ExistsInBaseType)
                                        .Where(static x => x.Type is not CodeType propertyType || propertyType.IsCollection || propertyType.TypeDefinition is not CodeClass)
                                        .OrderBy(static x => x, CodePropertyTypeBackwardComparer)
                                        .ThenBy(static x => x.Name))
        {
            writer.StartBlock($"{(includeElse ? "el" : string.Empty)}if self.{otherProp.Name}:");
            writer.WriteLine($"writer.{GetSerializationMethodName(otherProp.Type)}({NoneKeyword}, self.{otherProp.Name})");
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
                writer.StartBlock("else:");
            }
            var propertiesNames = complexProperties
                                .Select(static x => x.Name)
                                .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
                                .Select(static x => $"self.{x}")
                                .Aggregate(static (x, y) => $"{x}, {y}");
            writer.WriteLine($"writer.{GetSerializationMethodName(complexProperties[0].Type)}({NoneKeyword}, {propertiesNames})");
            if (includeElse)
            {
                writer.DecreaseIndent();
            }
        }
    }
    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid)
    {
        var nullablePrefix = code.ReturnType.IsNullable && !isVoid ? "Optional[" : string.Empty;
        var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? "]" : string.Empty;
        var returnRemark = isVoid ? $"Returns: {NoneKeyword}" : $"Returns: {nullablePrefix}{returnType}{nullableSuffix}";
        conventions.WriteLongDescription(code,
                                           writer,
                                           code.Parameters
                                               .Where(static x => x.Documentation.DescriptionAvailable)
                                               .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                               .Select(x => $"param {x.Name}: {x.Documentation.GetDescription(type => conventions.GetTypeString(type, code), normalizationFunc: PythonConventionService.RemoveInvalidDescriptionCharacters)}")
                                               .Union([returnRemark]));
        conventions.WriteDeprecationWarning(code, writer);
    }
    private static readonly PythonCodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid)
    {
        if (code.IsOfKind(CodeMethodKind.Factory))
            writer.WriteLine("@staticmethod");
        var accessModifier = conventions.GetAccessModifier(code.Access);
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
        var methodName = code.Kind switch
        {
            _ when code.IsAccessor => code.AccessedProperty?.Name,
            _ when isConstructor => "__init__",
            _ => code.Name,
        };
        var asyncPrefix = code.IsAsync && code.Kind is CodeMethodKind.RequestExecutor ? "async " : string.Empty;
        var instanceReference = code.IsOfKind(CodeMethodKind.Factory) ? string.Empty : "self,";
        var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer)
                                                        .Select(p => new PythonConventionService() // requires a writer instance because method parameters use inline type definitions
                                                        .GetParameterSignature(p, code, writer))
                                                        .ToList());
        var isStreamType = conventions.StreamTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
        var nullablePrefix = (code.ReturnType.IsNullable || isStreamType) && !isVoid ? "Optional[" : string.Empty;
        var nullableSuffix = (code.ReturnType.IsNullable || isStreamType) && !isVoid ? "]" : string.Empty;
        var propertyDecorator = code.Kind switch
        {
            CodeMethodKind.Getter => "@property",
            CodeMethodKind.Setter => $"@{methodName}.setter",
            _ => string.Empty
        };
        var nullReturnTypeSuffix = !isVoid && !isConstructor;
        var returnTypeSuffix = nullReturnTypeSuffix ? $"{nullablePrefix}{returnType}{nullableSuffix}" : NoneKeyword;
        if (!string.IsNullOrEmpty(propertyDecorator))
            writer.WriteLine($"{propertyDecorator}");
        writer.WriteLine($"{asyncPrefix}def {accessModifier}{methodName}({instanceReference}{parameters}) -> {returnTypeSuffix}:");
    }
    private string GetDeserializationMethodName(CodeTypeBase propType, CodeMethod codeElement, CodeClass parentClass)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.TranslateType(propType);
        if (conventions.TypeExistInSameClassAsTarget(propType, codeElement))
            propertyType = parentClass.Name;
        if (propType is CodeType currentType)
        {
            if (currentType.TypeDefinition is CodeEnum currentEnum)
                return $"get_{(currentEnum.Flags || isCollection ? "collection_of_enum_values" : "enum_value")}({propertyType.ToPascalCase()})";
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return $"get_collection_of_primitive_values({propertyType})";
                else
                    return $"get_collection_of_object_values({propertyType.ToPascalCase()})";
        }
        return propertyType switch
        {
            "str" or "bool" or "int" or "float" or "UUID" or "bytes" => $"get_{propertyType.ToLowerInvariant()}_value()",
            "datetime.datetime" => "get_datetime_value()",
            "datetime.date" => "get_date_value()",
            "datetime.time" => "get_time_value()",
            "datetime.timedelta" => "get_timedelta_value()",
            _ => $"get_object_value({propertyType.ToPascalCase()})",
        };
    }
    private string GetSerializationMethodName(CodeTypeBase propType)
    {
        var isCollection = propType.CollectionKind != CodeTypeBase.CodeTypeCollectionKind.None;
        var propertyType = conventions.TranslateType(propType);
        if (propType is CodeType currentType)
        {
            if (isCollection)
                if (currentType.TypeDefinition == null)
                    return "write_collection_of_primitive_values";
                else if (currentType.TypeDefinition is CodeEnum)
                    return "write_collection_of_enum_values";
                else
                    return "write_collection_of_object_values";
            else if (currentType.TypeDefinition is CodeEnum)
                return "write_enum_value";
        }
        return propertyType switch
        {
            "str" or "bool" or "int" or "float" or "UUID" or "bytes" => $"write_{propertyType.ToLowerInvariant()}_value",
            "datetime.datetime" => "write_datetime_value",
            "datetime.date" => "write_date_value",
            "datetime.time" => "write_time_value",
            "datetime.timedelta" => "write_timedelta_value",
            _ => "write_object_value",
        };
    }
    internal string GetTypeFactory(bool isVoid, bool isStream, bool isEnum, string returnType, bool isCollection)
    {
        if (isVoid) return string.Empty;
        if (isStream || isEnum) return $" \"{returnType}\",";
        if (conventions.IsPrimitiveType(returnType)) return isCollection ? $" {returnType}," : $" \"{returnType}\",";
        return $" {returnType},";
    }
    private string GetSendRequestMethodName(bool isVoid, bool isStream, bool isCollection, string returnType,
        bool isEnum)
    {
        if (isVoid) return "send_no_response_content_async";
        if (isCollection)
        {
            if (conventions.IsPrimitiveType(returnType) || isEnum) return "send_collection_of_primitive_async";
            return "send_collection_async";
        }

        if (isStream || conventions.IsPrimitiveType(returnType) || isEnum) return "send_primitive_async";
        return "send_async";
    }
    private void UpdateRequestInformationFromRequestBody(CodeMethod codeElement, RequestParams requestParams, CodeProperty requestAdapterProperty, LanguageWriter writer)
    {
        if (requestParams.requestBody != null)
        {
            var sanitizedRequestBodyContentType = codeElement.RequestBodyContentType.SanitizeDoubleQuote();
            if (requestParams.requestBody.Type.Name.Equals(conventions.StreamTypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (requestParams.requestContentType is not null)
                    writer.WriteLine($"{RequestInfoVarName}.set_stream_content({requestParams.requestBody.Name}, {requestParams.requestContentType.Name})");
                else if (!string.IsNullOrEmpty(sanitizedRequestBodyContentType))
                    writer.WriteLine($"{RequestInfoVarName}.set_stream_content({requestParams.requestBody.Name}, \"{sanitizedRequestBodyContentType}\")");
            }
            else
            {
                var setMethodName = requestParams.requestBody.Type is CodeType bodyType && (bodyType.TypeDefinition is CodeClass || bodyType.Name.Equals("MultipartBody", StringComparison.OrdinalIgnoreCase)) ? "set_content_from_parsable" : "set_content_from_scalar";
                writer.WriteLine($"{RequestInfoVarName}.{setMethodName}(self.{requestAdapterProperty.Name}, \"{sanitizedRequestBodyContentType}\", {requestParams.requestBody.Name})");
            }
        }
    }
}
