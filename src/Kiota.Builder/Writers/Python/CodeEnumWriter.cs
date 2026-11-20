using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Python;

public class CodeEnumWriter : BaseElementWriter<CodeEnum, PythonConventionService>
{
    public CodeEnumWriter(PythonConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine("from enum import Enum");
        writer.WriteLine();
        var baseType = codeElement.IsNumeric ? (codeElement.IsFractional ? "float" : "int") : "str";
        writer.WriteLine($"class {codeElement.Name}({baseType}, Enum):");
        conventions.WriteDeprecationWarning(codeElement, writer);
        writer.IncreaseIndent();
        if (!codeElement.Options.Any())
        {
            writer.WriteLine("pass");
        }
        else
        {
            codeElement.Options.ToList().ForEach(x =>
            {
                conventions.WriteInLineDescription(x, writer);
                if (codeElement.IsNumeric)
                {
                    if (codeElement.IsFractional)
                    {
                        if (!double.TryParse(x.WireName, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
                            throw new InvalidOperationException($"Invalid float enum value '{x.WireName}' for option '{x.Name}' in enum '{codeElement.Name}'.");
                        writer.WriteLine($"{x.Name} = {doubleVal.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                    }
                    else
                    {
                        if (!long.TryParse(x.WireName, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var longVal))
                            throw new InvalidOperationException($"Invalid integer enum value '{x.WireName}' for option '{x.Name}' in enum '{codeElement.Name}'.");
                        writer.WriteLine($"{x.Name} = {longVal.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                    }
                }
                else
                {
                    writer.WriteLine($"{x.Name} = \"{x.WireName.SanitizeDoubleQuote()}\",");
                }
            });
        }
    }
}
