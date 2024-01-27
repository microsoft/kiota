using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;

namespace Kiota.Builder.Writers.Dart;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, DartConventionService>
{
    public CodeMethodWriter(DartConventionService conventionService) : base(conventionService)
    {
    }

    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        throw new NotImplementedException();
    }
}
