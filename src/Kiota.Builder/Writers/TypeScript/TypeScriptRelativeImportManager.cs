using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;

public class TypescriptRelativeImportManager(string namespacePrefix, char namespaceSeparator) : RelativeImportManager(namespacePrefix, namespaceSeparator)
{
    private const string IndexFileName = "index.js";
    /// <summary>
    /// Returns the relative import path for the given using and import context namespace.
    /// </summary>
    /// <param name="codeUsing">The using to import into the current namespace context</param>
    /// <param name="currentNamespace">The current namespace</param>
    /// <returns>The import symbol, it's alias if any and the relative import path</returns>
    public override (string, string, string) GetRelativeImportPathForUsing(CodeUsing codeUsing, CodeNamespace currentNamespace)
    {
        if (codeUsing?.IsExternal ?? true)
            return (string.Empty, string.Empty, string.Empty);//it's an external import, add nothing

        var (importSymbol, typeDef) = codeUsing.Declaration?.TypeDefinition is CodeElement td ? td switch
        {
            CodeFunction f => (f.Name.ToFirstCharacterLowerCase(), td),
            _ => (td.Name.ToFirstCharacterUpperCase(), td),
        } : (codeUsing.Name, null);

        if (typeDef == null)
            return (importSymbol, codeUsing.Alias, $"./{IndexFileName}"); // it's relative to the folder, with no declaration (default failsafe)
        var importNamespace = typeDef.GetImmediateParentOfType<CodeNamespace>();
        var importPath = GetImportRelativePathFromNamespaces(currentNamespace, importNamespace);
        if (importPath.EndsWith('/'))
            importPath += IndexFileName;
        else
            importPath += ".js";
        return (importSymbol, codeUsing.Alias, importPath);
    }
}
