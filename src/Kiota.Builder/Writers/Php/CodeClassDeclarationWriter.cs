using System;
using System.Linq;
using System.Reflection.Metadata;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeClassDeclarationWriter: BaseElementWriter<CodeClass.Declaration, PhpConventionService>
    {

        public CodeClassDeclarationWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            conventions.WritePhpDocumentStart(writer);
            var parent = codeElement.Parent as CodeClass;
            // Only import promise if we are having a request executor method from the current class
            var requestExecutor = parent.GetMethodsOffKind(CodeMethodKind.RequestExecutor).FirstOrDefault();
            if ( requestExecutor != null)
            {
                codeElement.AddUsings(new CodeUsing()
                {
                    Alias = "Promise",
                    Declaration = new CodeType()
                    {
                        IsExternal = true,
                        IsNullable = false,
                        Name = "Http\\Promise"
                    },
                    Name = "Promise"
                }, new CodeUsing()
                {
                    Alias = "RejectedPromise",
                    Declaration = new CodeType()
                    {
                        IsExternal = true,
                        IsNullable = false,
                        Name = "Http\\Promise"
                    },
                    Name = "RejectedPromise"
                }, new CodeUsing()
                {
                    Alias = "Exception",
                    Declaration = new CodeType()
                    {
                        IsExternal = true,
                        IsNullable = false,
                        Name = ""
                    },
                    Name = "Exception"
                });
            }
            conventions.WriteNamespaceAndImports(codeElement, writer);
            //TODO: There is bug on creating filenames that makes file class names have multiple dots.
            // for example class user.LoginRequestBuilder 
            // instead of class LoginRequestBuilder
            var derivation = (codeElement?.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}") +
                             (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            writer.WriteLine($"class {codeElement.Name.Split('.').Last().ToFirstCharacterUpperCase()}{derivation} ");

            writer.WriteLine("{");
            writer.IncreaseIndent();
        }
    }
}
