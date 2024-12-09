using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class HttpRefiner(GenerationConfiguration configuration) : CommonLanguageRefiner(configuration)
{
    public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            CapitalizeNamespacesFirstLetters(generatedCode);
            ReplaceIndexersByMethodsWithParameter(
                generatedCode,
                false,
                static x => $"By{x.ToFirstCharacterUpperCase()}",
                static x => x.ToFirstCharacterUpperCase(),
                GenerationLanguage.HTTP);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceReservedNames(
                generatedCode,
                new HttpReservedNamesProvider(),
                x => $"{x}_escaped");
            RemoveCancellationParameter(generatedCode);
            ConvertUnionTypesToWrapper(
                generatedCode,
                _configuration.UsesBackingStore,
                static s => s
            );
            cancellationToken.ThrowIfCancellationRequested();
            SetBaseUrlForRequestBuilderMethods(generatedCode, GetBaseUrl(generatedCode));
            // Remove unused code from the DOM e.g Models, BarrelInitializers, e.t.c
            RemoveUnusedCodeElements(generatedCode);
        }, cancellationToken);
    }

    private string? GetBaseUrl(CodeElement element)
    {
        return element.GetImmediateParentOfType<CodeNamespace>()
                      .GetRootNamespace()?
                      .FindChildByName<CodeClass>(_configuration.ClientClassName)?
                      .Methods?
                      .FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.ClientConstructor))?
                      .BaseUrl;
    }

    private static void CapitalizeNamespacesFirstLetters(CodeElement current)
    {
        if (current is CodeNamespace currentNamespace)
            currentNamespace.Name = currentNamespace.Name.Split('.').Select(static x => x.ToFirstCharacterUpperCase()).Aggregate(static (x, y) => $"{x}.{y}");
        CrawlTree(current, CapitalizeNamespacesFirstLetters);
    }

    private static void SetBaseUrlForRequestBuilderMethods(CodeElement current, string? baseUrl)
    {
        if (baseUrl is not null && current is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            // Add a new property named BaseUrl and set its value to the baseUrl string
            var baseUrlProperty = new CodeProperty
            {
                Name = "BaseUrl",
                Kind = CodePropertyKind.Custom,
                Access = AccessModifier.Private,
                DefaultValue = baseUrl,
                Type = new CodeType { Name = "string", IsExternal = true }
            };
            codeClass.AddProperty(baseUrlProperty);
        }
        CrawlTree(current, (element) => SetBaseUrlForRequestBuilderMethods(element, baseUrl));
    }

    private void RemoveUnusedCodeElements(CodeElement element)
    {
        if (!IsRequestBuilderClass(element) || IsBaseRequestBuilder(element) || IsRequestBuilderClassWithoutAnyHttpOperations(element))
        {
            var parentNameSpace = element.GetImmediateParentOfType<CodeNamespace>();
            parentNameSpace?.RemoveChildElement(element);
        }
        else
        {
            // Add path variables
            AddPathParameters(element);
        }
        CrawlTree(element, RemoveUnusedCodeElements);
    }

    private static void AddPathParameters(CodeElement element)
    {
        // Target RequestBuilder Classes only
        if (element is not CodeClass codeClass) return;

        var parent = element.GetImmediateParentOfType<CodeNamespace>().Parent;
        while (parent is not null)
        {
            var codeIndexer = parent.GetChildElements(false)
                .OfType<CodeClass>()
                .FirstOrDefault()?
                .GetChildElements(false)
                .OfType<CodeMethod>()
                .FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility));

            if (codeIndexer is not null)
            {
                // Retrieve all the parameters of kind CodeParameterKind.Custom
                var customParameters = codeIndexer.Parameters
                    .Where(static param => param.IsOfKind(CodeParameterKind.Custom))
                    .ToList();

                // For each parameter:
                foreach (var param in customParameters)
                {
                    // Create a new property of kind CodePropertyKind.PathParameters using the parameter and add it to the codeClass
                    var pathParameterProperty = new CodeProperty
                    {
                        Name = param.Name,
                        Kind = CodePropertyKind.PathParameters,
                        Type = param.Type,
                        Access = AccessModifier.Public,
                        DefaultValue = param.DefaultValue,
                        SerializationName = param.SerializationName,
                        Documentation = param.Documentation
                    };
                    codeClass.AddProperty(pathParameterProperty);
                }
            }

            parent = parent.Parent?.GetImmediateParentOfType<CodeNamespace>();
        }
    }

    private static bool IsRequestBuilderClass(CodeElement element)
    {
        return element is CodeClass code && code.IsOfKind(CodeClassKind.RequestBuilder);
    }

    private bool IsBaseRequestBuilder(CodeElement element)
    {
        return element is CodeClass codeClass &&
            codeClass.Name.Equals(_configuration.ClientClassName, StringComparison.Ordinal);
    }

    private static bool IsRequestBuilderClassWithoutAnyHttpOperations(CodeElement element)
    {
        return element is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.RequestBuilder) &&
               !codeClass.Methods.Any(static method => method.IsOfKind(CodeMethodKind.RequestExecutor));
    }
}
