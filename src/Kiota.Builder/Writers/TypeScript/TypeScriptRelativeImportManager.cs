﻿using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class TypescriptRelativeImportManager : RelativeImportManager

{
    public TypescriptRelativeImportManager(string namespacePrefix, char namespaceSeparator) : base(namespacePrefix, namespaceSeparator)
    {
    }
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
            return (importSymbol, codeUsing.Alias, "./"); // it's relative to the folder, with no declaration (default failsafe)
        var importPath = GetImportRelativePathFromNamespaces(currentNamespace,
            typeDef.GetImmediateParentOfType<CodeNamespace>());
        var isCodeUsingAModel = codeUsing.Declaration?.TypeDefinition is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.Model);
        if (importPath == "./" && isCodeUsingAModel)
        {
            importPath += "index";
        }
        else if (string.IsNullOrEmpty(importPath))
            importPath += codeUsing.Name;
        else if (!isCodeUsingAModel)
        {
            importPath += (!string.IsNullOrEmpty(codeUsing.Declaration?.TypeDefinition?.Name) ? codeUsing.Declaration.TypeDefinition.Name : codeUsing.Declaration?.Name).ToFirstCharacterLowerCase();
        }
        return (importSymbol, codeUsing.Alias, importPath);
    }
}
