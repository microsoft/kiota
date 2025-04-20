using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.AL;

namespace Kiota.Builder.Refiners;
public class ALRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    private static ALReservedNamesProvider ReservedNamesProvider { get; } = new();
    public ALRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            RemoveUnusedMethods(generatedCode);
            RemoveAdditionalDataProperty(generatedCode); // we don't support additional data in AL (yet?)
            RemoveNotSupportedParameters(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            UpdateApiClientClass(generatedCode, _configuration);
            cancellationToken.ThrowIfCancellationRequested();
            MovePropertiesToMethods(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            UpdateModelClasses(generatedCode);
            UpdateRequestBuilderClasses(generatedCode, _configuration);
            UpdateRequestExecutorMethods(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            AddObjectProperties(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            AddAppJsonAsCodeFunction(generatedCode, _configuration);
            ModifyMethodName(generatedCode);
            UpdateMethodParameters(generatedCode);
        }, cancellationToken);
    }
    protected static void UpdateApiClientClass(CodeElement currentElement, GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (currentElement is CodeClass currentClass)
        {
            if (currentClass.Name.Equals(configuration.ClientClassName, StringComparison.OrdinalIgnoreCase))
            {
                var baseUrl = ALConfigurationHelper.GetBaseUrl(currentElement, configuration);
                ArgumentNullException.ThrowIfNull(baseUrl);
                currentClass.AddUsing(new CodeUsing { Name = "SimonOfHH.Kiota.Client" });
                currentClass.AddUsing(new CodeUsing { Name = "SimonOfHH.Kiota.Definitions" });
                currentClass.StartBlock.AddImplements(new CodeType { Name = "Kiota IApiClient SOHH", IsExternal = true });
                currentClass.AddVariable(ALVariableProvider.GetGlobalVariable("ReqConfig", new CodeType { Name = "codeunit \"Kiota ClientConfig SOHH\"", IsExternal = true }, "").ToVariable());
                currentClass.AddVariable(new ALVariable("APIAuthorization", new CodeType { Name = "codeunit \"Kiota API Authorization SOHH\"", IsExternal = true }, "", ""));
                currentClass.AddVariable(new ALVariable("Response_", new CodeType { Name = "codeunit System.RestClient.\"Http Response Message\"", IsExternal = true }, "", ""));
                currentClass.AddVariable(new ALVariable("BaseUrlLbl", new CodeType { Name = "Label" }, "", baseUrl));
                currentClass.AddVariable(new ALVariable("ConfigSet", new CodeType { Name = "Boolean" }, "", ""));
                currentClass.AddVariable(new ALVariable("AuthorizationNotInitializedErr", new CodeType { Name = "Label" }, "", "Authorization is uninitialized."));
                currentClass.AddMethod(ALVariableProvider.GetApiClientInitializerMethod(currentClass));
                currentClass.AddMethod(ALVariableProvider.GetApiClientConfigurationMethod(currentClass));
                currentClass.AddMethod(ALVariableProvider.GetApiClientConfigurationWithParameterMethod(currentClass));
                currentClass.AddMethod(ALVariableProvider.GetApiClientDefaultConfigurationMethod(currentClass));
                currentClass.AddMethod(ALVariableProvider.GetDefaultIApiClientMethods(currentClass).ToArray());
                currentClass.AddCustomProperty("client-class", "true");
            }
        }
        CrawlTree(currentElement, childElement => UpdateApiClientClass(childElement, configuration));
    }
    /// <summary>
    /// This method is used to remove unused methods from the current element.
    /// </summary>
    /// <param name="currentElement"></param>
    protected static void RemoveUnusedMethods(CodeElement currentElement)
    {
        if (currentElement is CodeClass codeClass)
        {
            codeClass.RemoveMethodByKinds(new[] { CodeMethodKind.Serializer, CodeMethodKind.Deserializer }); // we want to explicitly remove these methods and create our own
        }
        CrawlTree(currentElement, RemoveUnusedMethods);
    }

    protected static void AddAppJsonAsCodeFunction(CodeElement currentElement, GenerationConfiguration configuration)
    {
        if (currentElement is CodeNamespace currentNamespace)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            if (currentNamespace.Namespaces.First().Name.Contains('.', StringComparison.CurrentCulture))
                return;
            var childTypeCount = currentNamespace.GetTypeCounts();
            var function = new CodeFunction(new CodeMethod
            {
                Name = "AppJson",
                Access = AccessModifier.Public,
                IsStatic = true,
                Parent = new CodeClass
                {
                    Name = "Test",
                    Access = AccessModifier.Internal,
                    Parent = currentNamespace
                },
                ReturnType = new CodeType
                {
                    Name = "Text"
                },
            });
            function.AddUsing(new CodeUsing { Name = $"Name={configuration.ClientNamespaceName}" });
            function.AddUsing(new CodeUsing { Name = $"Publisher=SimonOfHH" }); // TODO-SF: Check if something can be provided via configuration
            function.AddUsing(new CodeUsing { Name = $"Brief=Auto-generated API Extension" });
            function.AddUsing(new CodeUsing { Name = $"Description=Auto-generated API Extension. Generated with Kiota" });
            function.AddUsing(new CodeUsing { Name = $"Version=0.0.0.1" });
            function.AddUsing(new CodeUsing { Name = $"HighestObjectID={childTypeCount.Where(x => (x.Key == "CodeClass") || (x.Key == "CodeEnum") || (x.Key == "ClassDeclaration")).ToDictionary().Values.Max()}" });
            currentNamespace.AddFunction(function);
        }
        CrawlTree(currentElement, childElement => AddAppJsonAsCodeFunction(childElement, configuration));
    }
    /// <summary>
    /// This method is used to update the method parameters of the current element.
    /// It checks if the parameter name is a reserved name and adds an underscore prefix if it is.
    /// </summary>
    /// <param name="currentElement"></param>
    protected static void UpdateMethodParameters(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod)
        {
            foreach (var parameter in currentMethod.Parameters)
            {
                if (ReservedNamesProvider.ReservedNames.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase))
                    parameter.Name = $"_{parameter.Name}";
            }
        }
        CrawlTree(currentElement, UpdateMethodParameters);
    }

    // Since we can't add methods with the same name to the DOM, in a previous step we added a "-overload" suffix to the method name.
    // This method removes the suffix from the method name.
    protected static void ModifyMethodName(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod)
        {
            if (currentMethod.Name.Contains("-overload", StringComparison.CurrentCulture))
                currentMethod.Name = currentMethod.Name.Replace("-overload", "", StringComparison.CurrentCulture);
        }
        CrawlTree(currentElement, ModifyMethodName);
    }
    protected static void RemoveAdditionalDataProperty(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass)
        {
            var additionalDataProperty = currentClass.Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.AdditionalData));
            if (additionalDataProperty != null)
                currentClass.RemoveChildElement(additionalDataProperty);
        }
        CrawlTree(currentElement, RemoveAdditionalDataProperty);
    }
    /// <summary>
    /// In AL, properties are not supported. We need to move them to getter/setter methods.
    /// </summary>
    /// <param name="currentElement"></param>
    protected static void MovePropertiesToMethods(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass)
        {
            var propertiesToMove = currentClass.Properties
                                                .Where(x => x.IsOfKind(CodePropertyKind.Custom) && x.GetCustomProperty("locked") != "true")
                                                .ToList();
            foreach (var property in propertiesToMove)
            {
                currentClass.RemoveChildElement(property);
                currentClass.AddMethod(property.ToCodeMethod());
                // TODO: Think about adding setter methods as well
            }
        }
        CrawlTree(currentElement, MovePropertiesToMethods);
    }
    protected static void RemoveNotSupportedParameters(CodeElement currentElement)
    {
        if (currentElement is CodeMethod currentMethod)
        {
            currentMethod.RemoveParametersByKind(new[] { CodeParameterKind.Cancellation, CodeParameterKind.RequestConfiguration });
        }
        CrawlTree(currentElement, RemoveNotSupportedParameters);
    }
    protected static void AddObjectProperties(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass)
            currentClass.AddProperty(ALVariableProvider.GetDefaultObjectProperties(currentClass).ToArray());
        if (currentElement is CodeEnum currentEnum)
            currentEnum.AddOption(ALVariableProvider.GetDefaultObjectProperties(currentEnum).ToArray());
        CrawlTree(currentElement, AddObjectProperties);
    }
    protected static void UpdateModelClasses(CodeElement currentElement)
    {
        if (currentElement is CodeClass currentClass)
        {
            if (currentClass.Kind != CodeClassKind.Model)
                return;
            currentClass.AddDefaultImplements();
            currentClass.RemoveInherits();
            currentClass.AddProperty(ALVariableProvider.GetDefaultGlobals(currentClass).ToArray());
            currentClass.AddUsing(new CodeUsing { Name = "SimonOfHH.Kiota.Definitions" });
            currentClass.AddUsing(new CodeUsing { Name = "SimonOfHH.Kiota.Utilities" });
            currentClass.AddMethod(ALVariableProvider.GetDefaultModelCodeunitMethods(currentClass).ToArray());
        }
        CrawlTree(currentElement, UpdateModelClasses);
    }
    protected static void UpdateRequestExecutorMethods(CodeElement currentElement)
    {
        if (currentElement is CodeMethod method)
        {
            if (method.IsOfKind(CodeMethodKind.RequestExecutor))
            {
                if (method.HttpMethod is not null)
                {
                    var conventionService = new ALConventionService();
                    if (conventionService.IsCodeunitType(method.ReturnType.GetTypeFromBase()))
                        method.AddCustomProperty("return-variable-name", "Target");
                    method.AddParameter(ALVariableProvider.GetLocalVariableP("RequestHandler", new CodeType { Name = "codeunit \"Kiota RequestHandler SoHH\"", IsExternal = true }, ""));
                }
                var codeClass = (CodeClass?)method.Parent;
                if (codeClass is not null)
                {
                    var requestConf = codeClass.InnerClasses.FirstOrDefault(c => c.Kind == CodeClassKind.QueryParameters && c.Name == $"{codeClass.Name}{method.Name}QueryParameters");
                    if (requestConf is not null)
                    {
                        foreach (var prop in requestConf.Properties)
                        {
                            method.AddParameter(prop.ToCodeParameter());
                        }
                    }
                }
            }
        }
        CrawlTree(currentElement, UpdateRequestExecutorMethods);
    }
    protected static void UpdateRequestBuilderClasses(CodeElement currentElement, GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var clientNamespace = ALConfigurationHelper.GetClientNamespace(currentElement, configuration);
        var modelNameSpace = ALConfigurationHelper.GetModelNamespace(currentElement, configuration);
        if (currentElement is CodeClass currentClass)
        {
            if (currentClass.Kind != CodeClassKind.RequestBuilder)
                return;
            currentClass.AddUsing(new CodeUsing { Name = modelNameSpace.Name });
            currentClass.AddUsing(new CodeUsing { Name = "SimonOfHH.Kiota.Client" });
            if (currentClass.Name != "ApiClient")
            {
                currentClass.AddVariable(ALVariableProvider.GetGlobalVariable("ReqConfig", new CodeType { Name = "codeunit \"Kiota ClientConfig SOHH\"", IsExternal = true }, "").ToVariable());
                currentClass.AddMethod(ALVariableProvider.GetSetConfigurationMethod(currentClass));
            }
            if (IsIndexerClass(currentClass, out CodeIndexer? indexer))
            {
                currentClass.AddVariable(ALVariableProvider.GetGlobalVariable("Identifier", new CodeType { TypeDefinition = indexer?.IndexParameter.Type }, "").ToVariable());
                currentClass.AddMethod(ALVariableProvider.GetIndexerClassSetIdentifierMethod(currentClass, indexer));
            }
            if (currentClass.Indexer is not null)
            {
                currentClass.AddMethod(currentClass.Indexer.ToCodeMethod());
                currentClass.AddUsing(new CodeUsing { Name = currentClass.Indexer.ReturnType.GetNamespaceName() });
            }
            foreach (var property in currentClass.Properties.Where(p => p.IsOfKind(CodePropertyKind.RequestBuilder)))
            {
                var propertyTypeNamespace = ((CodeType)property.Type).TypeDefinition?.GetImmediateParentOfType<CodeNamespace>();
                ArgumentNullException.ThrowIfNull(propertyTypeNamespace);
                if (!currentClass.Usings.Any(x => x.Name.Equals(propertyTypeNamespace.Name, StringComparison.OrdinalIgnoreCase)))
                    currentClass.AddUsing(new CodeUsing { Name = propertyTypeNamespace.Name });
                currentClass.RemoveChildElement(property);
                currentClass.AddMethod(property.ToCodeMethod());
            }
        }
        CrawlTree(currentElement, childElement => UpdateRequestBuilderClasses(childElement, configuration));
    }
    private static bool IsIndexerClass(CodeClass currentClass, out CodeIndexer? indexer)
    {
        indexer = null;
        if (currentClass.Kind != CodeClassKind.RequestBuilder)
            return false;
        if (currentClass.Parent is null)
            return false;
        if (currentClass.Parent.Parent is null) // first parent is the <item>namespace, second parent is the actual namespace
            return false;
        foreach (var child in currentClass.Parent.Parent.GetChildElements(true))
        {
            if (child is CodeClass codeClass)
            {
                if (codeClass.Indexer != null)
                {
                    if (((CodeType)codeClass.Indexer.ReturnType).TypeDefinition == currentClass)
                    {
                        indexer = codeClass.Indexer;
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
