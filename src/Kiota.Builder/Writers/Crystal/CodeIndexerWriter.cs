using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Crystal;
public class CodeIndexerWriter : BaseElementWriter<CodeIndexer, CrystalConventionService>
{
    public CodeIndexerWriter(CrystalConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        
        conventions.WriteLongDescription(codeElement, writer);
        conventions.WriteDeprecationAttribute(codeElement, writer);
        
        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var parameterType = conventions.GetTypeString(codeElement.IndexParameter.Type, codeElement);
        
        writer.WriteLine($"def []({codeElement.IndexParameter.Name.ToFirstCharacterLowerCase()} : {parameterType}) : {returnType}");
        writer.IncreaseIndent();
        writer.WriteLine($"raise NotImplementedError.new(\"Indexer not implemented for Crystal\")");
        writer.DecreaseIndent();
        writer.WriteLine("end");
        
        // CodeIndexer doesn't have a ReadOnly property, so we'll assume it's not read-only
        // This allows both getter and setter to be generated
        {
            writer.WriteLine($"def []=({codeElement.IndexParameter.Name.ToFirstCharacterLowerCase()} : {parameterType}, value : {returnType})");
            writer.IncreaseIndent();
            writer.WriteLine($"raise NotImplementedError.new(\"Indexer setter not implemented for Crystal\")");
            writer.DecreaseIndent();
            writer.WriteLine("end");
        }
    }
}
