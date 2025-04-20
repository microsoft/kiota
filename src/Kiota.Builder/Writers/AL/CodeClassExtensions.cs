using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.AL;
public static class CodeClassExtensions
{
    public static void AddVariable(this CodeClass codeClass, ALVariable variable)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        ArgumentNullException.ThrowIfNull(variable);
        var prop = variable.ToCodeProperty();
        prop.SetGlobalVariable(); // all variables on Class-level are global
        prop.AddCustomProperty("locked", "true"); // this prevents the variable from being changed by the further refinement process
        codeClass.AddProperty(prop);
    }
    public static IEnumerable<CodeProperty> ObjectProperties(this CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        return codeClass.Properties.Where(p1 => p1.IsObjectProperty());
    }
    public static string BaseUrlPartFromTemplate(this CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        var urlTemplateProperty = codeClass.Properties.FirstOrDefault(p => p.IsOfKind(CodePropertyKind.UrlTemplate));
        if (urlTemplateProperty is null)
            return string.Empty;
        var urlTemplate = urlTemplateProperty.DefaultValue;
        if (urlTemplate.Contains("{+baseurl}", StringComparison.OrdinalIgnoreCase))
            urlTemplate = urlTemplate.Replace("{+baseurl}", string.Empty, StringComparison.OrdinalIgnoreCase);
        urlTemplate = urlTemplate.Replace("\"", string.Empty, StringComparison.OrdinalIgnoreCase);
        while (urlTemplate.Count(x => x == '/') > 1)
        {
            var index = urlTemplate.IndexOf('/', StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                break;
            urlTemplate = urlTemplate.Remove(index, urlTemplate.IndexOf('/', index + 1) - index);
        }
        var regex = new System.Text.RegularExpressions.Regex(@"\{.*?\}");
        urlTemplate = regex.Replace(urlTemplate, string.Empty);
        return urlTemplate;
    }
    public static IEnumerable<CodeProperty> OrderedGlobalVariables(this CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        return codeClass.GlobalVariables().OrderBy(x => x.DefaultValue);
    }
    public static IEnumerable<CodeProperty> GlobalVariables(this CodeClass codeClass)
    {
        ArgumentNullException.ThrowIfNull(codeClass);
        return codeClass.Properties.Where(p1 => p1.IsGlobalVariable());
    }
    public static IEnumerable<CodeMethod> GetPropertyMethods(this CodeClass parentClass)
    {
        ArgumentNullException.ThrowIfNull(parentClass);
        return parentClass.Methods.Where(x => x.IsPropertyMethod());
    }
    public static void AddDefaultImplements(this CodeClass currentClass)
    {
        ArgumentNullException.ThrowIfNull(currentClass);
        currentClass.StartBlock.RemoveImplements(currentClass.StartBlock.Implements.ToArray());
        currentClass.StartBlock.AddImplements(new CodeType { Name = "Kiota IModelClass SOHH", IsExternal = true });
    }
    public static void RemoveInherits(this CodeClass currentClass)
    {
        ArgumentNullException.ThrowIfNull(currentClass);
        if (currentClass.StartBlock.Inherits is not null) // leave a note in the documentation that the class inherits from another class, but we don't support that yet
        {
            if (currentClass.Documentation == null)
                currentClass.Documentation = new CodeDocumentation();
            currentClass.Documentation.DescriptionTemplate += $"{(!String.IsNullOrEmpty(currentClass.Documentation.DescriptionTemplate) ?
                                                                (currentClass.Documentation.DescriptionTemplate.EndsWith('.'.ToString(), StringComparison.OrdinalIgnoreCase) ? string.Empty : ".") : string.Empty)} Generator Info: Inherits from {currentClass.StartBlock.Inherits.Name}";
            currentClass.StartBlock.Inherits = null;
        }
    }
}