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
        writer.CloseBlock();
    }
}
