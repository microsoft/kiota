using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Swift {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, SwiftConventionService>
    {
        public CodePropertyWriter(SwiftConventionService conventionService) : base(conventionService) {}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            var propertyName = codeElement.Access == AccessModifier.Public ? codeElement.Name.ToFirstCharacterUpperCase() : codeElement.Name.ToFirstCharacterLowerCase();
            var returnType = conventions.GetTypeString(codeElement.Type, codeElement.Parent);
            switch(codeElement.Kind) {
                default:
                    writer.WriteLine($"var {propertyName}: {returnType}?");
                break;
            }
        }
    }
}
