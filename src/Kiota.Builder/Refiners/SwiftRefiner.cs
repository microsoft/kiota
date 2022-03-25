using System;
using System.Linq;
using Kiota.Builder.Extensions;
namespace Kiota.Builder.Refiners;
public class SwiftRefiner : CommonLanguageRefiner
{
    public SwiftRefiner(GenerationConfiguration configuration) : base(configuration) {}
    public override void Refine(CodeNamespace generatedCode)
    {
        CapitalizeNamespacesFirstLetters(generatedCode);
        AddRootClassForExtensions(generatedCode);
        ReplaceIndexersByMethodsWithParameter(
            generatedCode,
            generatedCode,
            false,
            "ById");
        ReplaceReservedNames(
            generatedCode,
            new SwiftReservedNamesProvider(),
            x => $"{x}_escaped");
        RemoveCancellationParameter(generatedCode);
        ConvertUnionTypesToWrapper(
            generatedCode,
            _configuration.UsesBackingStore
        );
        AddPropertiesAndMethodTypesImports(
            generatedCode,
            true,
            false,
            true);
    }

    private static void CapitalizeNamespacesFirstLetters(CodeElement current) {
        if(current is CodeNamespace currentNamespace)
            currentNamespace.Name = currentNamespace.Name?.Split('.')?.Select(x => x.ToFirstCharacterUpperCase())?.Aggregate((x, y) => $"{x}.{y}");
        CrawlTree(current, CapitalizeNamespacesFirstLetters);
    }
    private void AddRootClassForExtensions(CodeElement current) {
        if(current is CodeNamespace currentNamespace &&
            currentNamespace.FindNamespaceByName(_configuration.ClientNamespaceName) is CodeNamespace clientNamespace) {
            clientNamespace.AddClass(new CodeClass {
                Name = clientNamespace.Name.Split('.', StringSplitOptions.RemoveEmptyEntries).Last().ToFirstCharacterUpperCase(),
                Kind = CodeClassKind.BarrelInitializer,
                Description = "Root class for extensions",
            });
        }
    }
}
