using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, RubyConventionService>
    {
        public CodePropertyWriter(RubyConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            //TODO
        }
    }
}
