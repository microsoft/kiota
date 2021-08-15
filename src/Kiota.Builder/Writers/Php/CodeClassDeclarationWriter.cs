using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeClassDeclarationWriter: BaseElementWriter<CodeClass.Declaration, PhpConventionService>
    {

        public CodeClassDeclarationWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            writer.WriteLines("<?php", string.Empty); 
            //writer.IncreaseIndent();
            bool hasUse = false;
            foreach (var codeUsing in codeElement.Usings
                .Where(x => x.Declaration.IsExternal)
                .Distinct()
                .GroupBy(x => x.Declaration?.Name)
                .OrderBy(x => x.Key)
            )
            {
                hasUse = true;
                writer.WriteLine($"use {codeUsing.Key.ToCamelCase()};");
            }

            if (hasUse)
            {
                writer.WriteLine(string.Empty);
            }
            var derivation = (codeElement.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}") +
                             (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation} ");
            writer.WriteLine("{");
            writer.IncreaseIndent();
        }
    }
}
