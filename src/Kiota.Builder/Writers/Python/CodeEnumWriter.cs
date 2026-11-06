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
        var baseType = codeElement.IsNumeric ? (string.Equals(codeElement.Format, "float", StringComparison.OrdinalIgnoreCase) || string.Equals(codeElement.Format, "double", StringComparison.OrdinalIgnoreCase) ? "float" : "int") : "str";
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
                    writer.WriteLine($"{x.Name} = {x.WireName},");
                }
                else
                {
                    writer.WriteLine($"{x.Name} = \"{x.WireName.SanitizeDoubleQuote()}\",");
                }
            });
        }
    }
}
