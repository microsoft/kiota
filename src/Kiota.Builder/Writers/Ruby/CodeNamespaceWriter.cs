using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Ruby;

public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, RubyConventionService>
{
    private readonly RubyPathSegmenter PathSegmenter;
    private readonly string? ClientNamespaceName;
    public CodeNamespaceWriter(RubyConventionService conventionService, RubyPathSegmenter pathSegmenter, string? clientNamespaceName = null) : base(conventionService)
    {
        ArgumentNullException.ThrowIfNull(pathSegmenter);
        PathSegmenter = pathSegmenter;
        ClientNamespaceName = clientNamespaceName;
    }
    public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        // registering instead of requiring lets a class load its base on demand, in any order
        var autoloaded = new List<CodeElement>(codeElement.GetChildElements(true).OfType<CodeEnum>()
                                                        .Where(RubyConventionService.IsAutoloaded)
                                                        .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase));
        NamespaceClassNamesProvider.WriteClassesInOrderOfInheritance(codeElement, x =>
        {
            if (RubyConventionService.IsAutoloaded(x)) autoloaded.Add(x);
        });
        var isClientNamespace = codeElement.Name.Equals(ClientNamespaceName, StringComparison.OrdinalIgnoreCase);
        if (autoloaded.Count == 0 && !isClientNamespace) return;
        writer.WriteLine(RubyConventionService.FrozenStringLiteralComment);
        writer.WriteLine();
        conventions.WriteNamespaceModules(codeElement, writer);
        foreach (var element in autoloaded)
            writer.WriteLine($"autoload :{element.Name.ToFirstCharacterUpperCase()}, ::File.expand_path({RubyConventionService.ToRubyStringLiteral(PathSegmenter.GetRelativeFileName(codeElement, element).ToSnakeCase())}, __dir__)");
        if (isClientNamespace)
            WriteEagerLoad(writer);
        conventions.WriteNamespaceClosing(codeElement, writer);
    }
    private static void WriteEagerLoad(LanguageWriter writer)
    {
        RubyConventionService.WriteMemberSeparator(writer);
        writer.WriteLine("# Loads every model and enum registered for autoload, for example before a server forks.");
        writer.StartBlock("def self.eager_load!");
        // a loaded file can register autoloads in a module already walked, so walk again until nothing loads
        writer.StartBlock("loop do");
        writer.WriteLines("loaded = false", "pending = [self]", "seen = {}");
        writer.StartBlock("until pending.empty?");
        writer.WriteLines("mod = pending.pop", "next if seen.key?(mod)");
        writer.WriteLine();
        writer.WriteLine("seen[mod] = true");
        writer.StartBlock("mod.constants(false).each do |name|");
        writer.WriteLines("loaded = true if mod.autoload?(name, false)", "value = mod.const_get(name, false)", "pending << value if value.instance_of?(::Module)");
        writer.CloseBlock("end");
        writer.CloseBlock("end");
        writer.WriteLine("break unless loaded");
        writer.CloseBlock("end");
        writer.CloseBlock("end");
    }
}
