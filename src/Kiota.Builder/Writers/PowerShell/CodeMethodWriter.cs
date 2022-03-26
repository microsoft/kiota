using System;
using System.Linq;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Extensions;

namespace Kiota.Builder.Writers.PowerShell
{
    public class CodeMethodWriter : CSharp.CodeMethodWriter
    {
        public CodeMethodWriter(CSharpConventionService conventionService) : base(conventionService) { }

        //public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
        //{
        //    base.WriteCodeElement(codeElement, writer);
        //}
        protected override void WriteCommandBuilderBody(CodeMethod codeMethod, RequestParams requestParams, bool isVoid, string returnType, LanguageWriter writer)
        {
            var parentClass = codeMethod.Parent as CodeClass;
            var urlTemplateProperty = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate);
            var requestExecutor = parentClass.GetMethodsOffKind(CodeMethodKind.RequestExecutor).FirstOrDefault();
            switch (codeMethod.Name)
            {
                case "BeginProcessing":
                    WriteBeginProcessing(urlTemplateProperty, writer);
                    break;
                case "EndProcessing":
                    WriteEndProcessing(writer);
                    break;
                case "StopProcessing":
                    WriteStopProcessing(writer);
                    break;
                case "ProcessRecord":
                    WriteProcessRecord(writer);
                    break;
                case "ProcessRecordAsync":
                    WriteProcessRecordAsync(parentClass, requestExecutor, writer);
                    break;
                default:
                    throw new NotImplementedException(codeMethod.Name);
            }
        }

        private void WriteProcessRecordAsync(CodeClass parentClass, CodeMethod requestExecutor, LanguageWriter writer)
        {
            writer.WriteLine($"// Performs execution of the command.");
            writer.WriteLine($"await {requestExecutor.Name}();");
        }

        private void WriteProcessRecord(LanguageWriter writer)
        {
            writer.WriteLine($"// TODO: Implement PowerShell AsyncCommandRuntime to call ProcessRecordAsync async.");
            writer.WriteLine("try {");
            writer.IncreaseIndent();
            writer.WriteLine("ProcessRecordAsync();");
            writer.CloseBlock();
            writer.WriteLine("catch (Exception exception){");
            writer.IncreaseIndent();
            writer.WriteLine("WriteError(new ErrorRecord(exception,string.Empty,ErrorCategory.NotSpecified,null));");
            writer.CloseBlock();
        }

        private void WriteStopProcessing(LanguageWriter writer)
        {
            writer.WriteLine($"// Interrupts currently running code within the command.");
            writer.WriteLine($"base.StopProcessing();");
        }

        private void WriteEndProcessing(LanguageWriter writer)
        {
            writer.WriteLine($"// Performs clean-up after the command execution.");
        }

        private void WriteBeginProcessing(CodeProperty urlTemplateProperty, LanguageWriter writer)
        {
            writer.WriteLine($"UrlTemplate = {urlTemplateProperty.DefaultValue};");
        }
    }
}
