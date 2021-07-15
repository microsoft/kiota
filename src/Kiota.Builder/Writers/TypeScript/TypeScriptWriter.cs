namespace Kiota.Builder.Writers.TypeScript
{
    public class TypeScriptWriter : LanguageWriter
    {
        public TypeScriptWriter(string rootPath, string clientNamespaceName, bool usesBackingStore)
        {
            PathSegmenter = new TypeScriptPathSegmenter(rootPath,clientNamespaceName);
            var conventionService = new TypeScriptConventionService(null);
            AddCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
            AddCodeElementWriter(new CodeClassEndWriter());
            AddCodeElementWriter(new CodeEnumWriter(conventionService));
            AddCodeElementWriter(new CodeMethodWriter(conventionService, usesBackingStore));
            AddCodeElementWriter(new CodePropertyWriter(conventionService));
            AddCodeElementWriter(new CodeTypeWriter(conventionService));
        }
    }
}
