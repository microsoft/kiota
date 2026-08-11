using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;

public class HttpRefiner(GenerationConfiguration configuration) : CommonLanguageRefiner(configuration)
{
    private const string BaseUrl = "BaseUrl";
    private const string BaseUrlName = "string";
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
            AddPathParameters(generatedCode);
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
                Name = BaseUrl,
                Kind = CodePropertyKind.Custom,
                Access = AccessModifier.Private,
                DefaultValue = baseUrl,
                Type = new CodeType { Name = BaseUrlName, IsExternal = true }
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
        CrawlTree(element, RemoveUnusedCodeElements);
    }

    private void AddPathParameters(CodeElement element)
    {
        var parent = element.GetImmediateParentOfType<CodeNamespace>().Parent;
        while (parent is not null)
        {
            var codeIndexer = parent.GetChildElements(false)
                .OfType<CodeClass>()
                .FirstOrDefault()?
                .GetChildElements(false)
                .OfType<CodeMethod>()
                .FirstOrDefault(static x => x.IsOfKind(CodeMethodKind.IndexerBackwardCompatibility));

            if (codeIndexer is not null && element is CodeClass codeClass)
            {
                // Retrieve all the parameters of kind CodeParameterKind.Custom
                var customProperties = codeIndexer.Parameters
                    .Where(static x => x.IsOfKind(CodeParameterKind.Custom))
                    .Select(x => new CodeProperty
                    {
                        Name = x.Name,
                        Kind = CodePropertyKind.PathParameters,
                        Type = x.Type,
                        Access = AccessModifier.Public,
                        DefaultValue = x.DefaultValue,
                        SerializationName = x.SerializationName,
                        Documentation = x.Documentation
                    })
                    .ToArray();

                if (customProperties.Length > 0)
                {
                    codeClass.AddProperty(customProperties);
                }
            }

            parent = parent.Parent?.GetImmediateParentOfType<CodeNamespace>();
        }
        CrawlTree(element, AddPathParameters);
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
