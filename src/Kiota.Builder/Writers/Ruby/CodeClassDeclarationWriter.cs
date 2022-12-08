using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace  Kiota.Builder.Writers.Ruby {
    public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, RubyConventionService>
    {
        private readonly RelativeImportManager relativeImportManager;
        public CodeClassDeclarationWriter(RubyConventionService conventionService, string clientNamespaceName) : base(conventionService){
            relativeImportManager = new RelativeImportManager(clientNamespaceName, '.');
        }
        
        
        public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
        {
            ArgumentNullException.ThrowIfNull(codeElement);
            ArgumentNullException.ThrowIfNull(writer);
            var currentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
            if(codeElement?.Parent?.Parent is not CodeClass) {
                foreach (var codeUsing in codeElement.Usings
                                            .Where(x => x.IsExternal)
                                            .Distinct()
                                            .GroupBy(x => x.Declaration?.Name)
                                            .OrderBy(x => x.Key))
                    writer.WriteLine($"require '{codeUsing.Key.ToSnakeCase()}'");
                        
                foreach (var relativePath in codeElement.Usings
                                            .Where(x => !x.IsExternal)
                                            .Select(x => x.Declaration?.Name?.StartsWith('.') ?? false ? 
                                                (string.Empty, string.Empty, x.Declaration.Name) :
                                                relativeImportManager.GetRelativeImportPathForUsing(x, currentNamespace))
                                            .Select(x => x.Item3)
                                            .Distinct()
                                            .OrderBy(x => x))
                    writer.WriteLine($"require_relative '{relativePath.ToSnakeCase()}'");
            }
            writer.WriteLine();
            if(codeElement?.Parent?.Parent is CodeNamespace ns) {
                writer.WriteLine($"module {ns.Name.NormalizeNameSpaceName("::")}");
                writer.IncreaseIndent();
            }
    
            var derivation = codeElement.Inherits == null ? string.Empty : $" < {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}";
            conventions.WriteShortDescription((codeElement.Parent as CodeClass).Documentation.Description, writer);
            writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation}");
            writer.IncreaseIndent();
            var mixins = !codeElement.Implements.Any() ? string.Empty : $"include {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}";
            writer.WriteLine($"{mixins}");
        }
    }
}
