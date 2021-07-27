using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, GoConventionService>
    {
        public CodePropertyWriter(GoConventionService conventionService) : base(conventionService) {}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            var propertyName = codeElement.Access == AccessModifier.Public ? codeElement.Name.ToFirstCharacterUpperCase() : codeElement.Name.ToFirstCharacterLowerCase();
            var returnType = conventions.GetTypeString(codeElement.Type);
            writer.WriteLine($"{propertyName} {returnType};"); //TODO request builders with bodies
        }
    }
}
