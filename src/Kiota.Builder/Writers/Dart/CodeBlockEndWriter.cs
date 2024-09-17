using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;

namespace Kiota.Builder.Writers.Dart;
public class CodeBlockEndWriter : BaseElementWriter<BlockEnd, DartConventionService>
{
    public CodeBlockEndWriter(DartConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(BlockEnd codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        overrideCloneMethod(codeElement, writer);
        writer.CloseBlock();
    }

    private void overrideCloneMethod(BlockEnd codeElement, LanguageWriter writer)
    {
        if (codeElement?.Parent is CodeClass classElement && classElement.Kind is CodeClassKind.RequestBuilder)
        {
            writer.WriteLine("@override");
            writer.WriteLine($"{classElement.Name.ToFirstCharacterUpperCase()} clone() {{");
            writer.IncreaseIndent();
            var constructor = classElement.GetMethodsOffKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor).Where(static x => x.Parameters.Any()).FirstOrDefault();
            String? argumentList = constructor?.Parameters.OrderBy(x => x, new BaseCodeParameterOrderComparer()).Select(static x => x.Name).Aggregate(static (x, y) => $"{x}, {y}");
            writer.WriteLine($"return {classElement.Name.ToFirstCharacterUpperCase()}({argumentList});");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
