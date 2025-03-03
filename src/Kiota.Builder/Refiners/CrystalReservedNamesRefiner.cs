using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners
{
    public class CrystalReservedNamesRefiner : CommonLanguageRefiner
    {
        public CrystalReservedNamesRefiner(GenerationConfiguration configuration) : base(configuration)
        {
        }

        public void Refine(CodeNamespace generatedCode)
        {
            System.ArgumentNullException.ThrowIfNull(generatedCode);

            var reservedNames = generatedCode.GetChildElements(true)
                                             .SelectMany(x => x.GetChildElements(true))
                                             .Where(x => CrystalReservedNamesProvider.IsReserved(x.Name))
                                             .ToList();

            foreach (var element in reservedNames)
            {
                element.Name = $"{element.Name}_";
            }
        }

        public override Task RefineAsync(CodeNamespace generatedCode, CancellationToken cancellationToken)
        {
            Refine(generatedCode);
            return Task.CompletedTask;
        }
    }
}
