using System.Linq;
using Kiota.Builder.Extensions;
namespace Kiota.Builder.Refiners {
    public class SwiftRefiner : CommonLanguageRefiner
    {
        public SwiftRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            CapitalizeNamespacesFirstLetters(generatedCode);
            ReplaceReservedNames(
                generatedCode,
                new SwiftReservedNamesProvider(),
                x => $"{x}_escaped");
        }

        private static void CapitalizeNamespacesFirstLetters(CodeElement current) {
            if(current is CodeNamespace currentNamespace)
                currentNamespace.Name = currentNamespace.Name?.Split('.')?.Select(x => x.ToFirstCharacterUpperCase())?.Aggregate((x, y) => $"{x}.{y}");
            CrawlTree(current, CapitalizeNamespacesFirstLetters);
        }
    }
}
