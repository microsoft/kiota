using System;
using System.ComponentModel;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Go;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeNameSpaceWriter : BaseElementWriter<CodeNamespace, TypeScriptConventionService>
{
    public CodeNameSpaceWriter(TypeScriptConventionService conventionService) : base(conventionService) { }
    private TypeScriptConventionService? localConventions;
    /// <summary>
    /// Writes export statements for classes and enums belonging to a namespace into a generated index.ts file. 
    /// The classes should be export in the order of inheritance so as to avoid circular dependency issues in javascript.
    /// </summary>
    /// <param name="codeElement">Code element is a code namespace</param>
    /// <param name="writer"></param>
    public override void WriteCodeElement(CodeNamespace codeNamespace, LanguageWriter writer)
    {
        foreach (var codeFunction in codeNamespace.Functions)
        {
            writer.WriteLine($"export * from './{codeFunction.Name.ToFirstCharacterLowerCase()}'");
        }

        foreach (var e in codeNamespace.Enums)
        {
            writer.WriteLine($"export * from './{e.Name.ToFirstCharacterLowerCase()}'");
        }
        foreach (var c in codeNamespace.CodeInterfaces)
        {
            writer.WriteLine($"export * from './{c.Name.ToFirstCharacterLowerCase()}'");
        }
        localConventions = new TypeScriptConventionService(writer);
        var requestBuilder = codeNamespace.Classes.FirstOrDefault(x => x.Kind == CodeClassKind.RequestBuilder);

        if (!string.IsNullOrWhiteSpace(codeNamespace?.Parent?.Name) && requestBuilder != null)
        {
            WriteDeclarationForModuleAugmentation(requestBuilder, writer);
        }
    }

    private static string getCurrentRequestBuilderAlias(string name) {
        return $"Aliased{name}";
    }
    private static (CodeClass?, bool) GetReferingParentRequestBuilderAndDepth(CodeClass childRequestBuilder)
    {
        if (childRequestBuilder.Parent?.Parent is CodeNamespace callingRequestBuilderParentNamespace)
        {

            while (callingRequestBuilderParentNamespace is CodeNamespace)
            {
                var reqBuilderUsingIndexer = callingRequestBuilderParentNamespace.Classes.Where(x => x.Kind == CodeClassKind.RequestBuilder).
                    FirstOrDefault(x => x.Methods.Any(y => y.Kind == CodeMethodKind.IndexerBackwardCompatibility && y.ReturnType is CodeType codeType && codeType.TypeDefinition == childRequestBuilder));

                if (reqBuilderUsingIndexer != null)
                {
                    return (reqBuilderUsingIndexer, true);
                }
                else { 
                    var reqBuilderUsingPropGetter = callingRequestBuilderParentNamespace.Classes.Where(x => x.Kind == CodeClassKind.RequestBuilder).
                    FirstOrDefault(x => x.Properties.Any(y => y.Kind == CodePropertyKind.RequestBuilder &&  y.Type is CodeType codeType && codeType.TypeDefinition == childRequestBuilder));

                    if (reqBuilderUsingPropGetter != null) 
                    {
                        return (reqBuilderUsingPropGetter, false);
                    }
                }
#nullable disable
                callingRequestBuilderParentNamespace = callingRequestBuilderParentNamespace.Parent as CodeNamespace;

            }
        }
        return (null, false);
    }

    private void WriteImportsRequiredForModelAugmentation(CodeClass requestBuilder, CodeClass referencingRequestBuilder, bool isReferencedByIndexer, LanguageWriter writer) 
    {
        var refRelativeDepthString = isReferencedByIndexer ? "../" : string.Empty;
        var requestBuilderNameAlias = string.Equals(requestBuilder.Name, referencingRequestBuilder.Name, StringComparison.OrdinalIgnoreCase) ? getCurrentRequestBuilderAlias(requestBuilder.Name) : string.Empty;
        writer.WriteLine($"import {{{requestBuilder.Name.ToFirstCharacterUpperCase()} {(!string.IsNullOrWhiteSpace(requestBuilderNameAlias)? " as " + requestBuilderNameAlias : string.Empty)}}} from \"./{requestBuilder.Name.ToFirstCharacterLowerCase()}\"");
        writer.WriteLine($"import {{{referencingRequestBuilder.Name.ToFirstCharacterUpperCase()}}} from \"{refRelativeDepthString}../{referencingRequestBuilder.Name.ToFirstCharacterLowerCase()}\"");
        if (isReferencedByIndexer)
        {
            writer.WriteLine("import { getPathParameters } from \"@microsoft/kiota-abstractions\";");
        }
    }

    //private void WriteReflective
    private void WriteModuleAugmentationDeclaration(CodeClass requestBuilder, CodeClass referencingRequestBuilder, bool isReferencedByIndexer, LanguageWriter writer)
    {
        var refRelativeDepthString = isReferencedByIndexer ? "../" : string.Empty;
        var requestBuilderNameAlias = string.Equals(requestBuilder.Name, referencingRequestBuilder.Name, StringComparison.OrdinalIgnoreCase) ? getCurrentRequestBuilderAlias(requestBuilder.Name) : requestBuilder.Name;
        writer.WriteLine($"declare module \"{refRelativeDepthString}../{referencingRequestBuilder.Name.ToFirstCharacterLowerCase()}\"{{");
        writer.IncreaseIndent();
        writer.WriteLine($"interface {referencingRequestBuilder.Name.ToFirstCharacterUpperCase()}{{");
        writer.IncreaseIndent();
        if (isReferencedByIndexer)
        {
            var referencingMethod = referencingRequestBuilder.Methods.FirstOrDefault(x => x.ReturnType is CodeType codeType && codeType.TypeDefinition == requestBuilder);
            writer.WriteLine($"{referencingMethod?.Name.ToFirstCharacterLowerCase()}:(id : string) => {requestBuilderNameAlias.ToFirstCharacterUpperCase()}");
        }
        else 
        {
            var property = referencingRequestBuilder.Properties.FirstOrDefault(x => x.Kind == CodePropertyKind.RequestBuilder && x.Type is CodeType codeType && codeType.TypeDefinition == requestBuilder);
            writer.WriteLine($"{property?.Name.ToFirstCharacterLowerCase()}:{requestBuilderNameAlias.ToFirstCharacterUpperCase()}");
        }
        writer.DecreaseIndent();
        writer.WriteLine("}");
        writer.DecreaseIndent();
        writer.WriteLine("}");
    }
    private void WriteDeclarationForModuleAugmentation(CodeClass requestBuilder, LanguageWriter writer)
    {

        var referencingMethodData = GetReferingParentRequestBuilderAndDepth(requestBuilder);
        var isReferencedByIndexer = referencingMethodData.Item2;
        
        var referencingRequestBuilder = referencingMethodData.Item1;
       
        if (referencingRequestBuilder != null)
        {
            WriteImportsRequiredForModelAugmentation(requestBuilder, referencingRequestBuilder, isReferencedByIndexer, writer);
            WriteModuleAugmentationDeclaration(requestBuilder, referencingRequestBuilder, isReferencedByIndexer, writer);
            if (isReferencedByIndexer)
            {
                WriteReflectionForIndexerType(requestBuilder, referencingRequestBuilder, writer);
            }
            else 
            {
                WriteReflectionForPropertyType(requestBuilder, referencingRequestBuilder, writer);
            }
            //if (isReferencedByIndexer)
            //{
            //    writer.WriteLine($"Reflect.defineProperty({parentRequestBuilderName}.prototype, \"{requestBuilder.Name.Split("RequestBuilder")[0].ToFirstCharacterLowerCase()}\", {{");
            //    writer.IncreaseIndent();
            //    writer.WriteLine("configurable: true,");
            //    writer.WriteLine("enumerable: true,");
            //    writer.WriteLine($"get: function(this: {parentRequestBuilderName}, id:String) {{");
            //    writer.IncreaseIndent();
            //    writer.WriteLine("const urlTplParams = getPathParameters(this.pathParameters);\r\n urlTplParams[\"attachment%2Did\"] = id");

            //    writer.WriteLine($"return new {childRequestBuilderAlias}(this.pathParameters,this.requestAdapter)");
            //    writer.DecreaseIndent();
            //    //writer.WriteLine($"}} as (id) => {childRequestBuilderAlias}");
            //    writer.WriteLine($"}} as any");
            //    writer.DecreaseIndent();
            //    writer.WriteLine("})");
            //}
            //else
            //{
            //    writer.WriteLine($"Reflect.defineProperty({parentRequestBuilderName}.prototype, \"{requestBuilder.Name.Split("RequestBuilder")[0].ToFirstCharacterLowerCase()}\", {{");
            //    writer.IncreaseIndent();
            //    writer.WriteLine("configurable: true,");
            //    writer.WriteLine("enumerable: true,");
            //    writer.WriteLine($"get: function(this: {parentRequestBuilderName}) {{");
            //    writer.IncreaseIndent();
            //    writer.WriteLine($"return new {childRequestBuilderAlias}(this.pathParameters,this.requestAdapter)");

            //    writer.DecreaseIndent();
            //    writer.WriteLine("}");
            //    writer.DecreaseIndent();
            //    writer.WriteLine("})");
            //}


        }


    }

    private void WriteReflectionForIndexerType(CodeClass requestBuilder, CodeClass referencingRequestBuilder, LanguageWriter writer) {
        var referencingMethod = referencingRequestBuilder.Methods.FirstOrDefault(x => x.ReturnType is CodeType codeType && codeType.TypeDefinition == requestBuilder);
        var requestBuilderNameAlias = string.Equals(requestBuilder.Name, referencingRequestBuilder.Name, StringComparison.OrdinalIgnoreCase) ? getCurrentRequestBuilderAlias(requestBuilder.Name) : requestBuilder.Name;
       writer.WriteLine($"Reflect.defineProperty({referencingRequestBuilder.Name.ToFirstCharacterUpperCase()}.prototype, \"{referencingMethod.Name.ToFirstCharacterLowerCase()}\", {{");
        writer.IncreaseIndent();
        writer.WriteLine("configurable: true,");
        writer.WriteLine("enumerable: true,");
        writer.WriteLine($"get: function() {{");
        writer.IncreaseIndent();
        writer.WriteLine($"return function(this: {referencingRequestBuilder.Name.ToFirstCharacterUpperCase()}, id: string){{");
        writer.IncreaseIndent();
        WriteIndexerBody(referencingMethod, requestBuilder, "", writer);
        writer.WriteLine($"return new {requestBuilderNameAlias.ToFirstCharacterUpperCase()}(urlTplParams,this.requestAdapter)");
        writer.DecreaseIndent();
        writer.WriteLine("}");
        writer.DecreaseIndent();

        writer.DecreaseIndent();
        writer.WriteLine("}");
        writer.DecreaseIndent();
        writer.WriteLine("})");
    }
    private static void WriteReflectionForPropertyType(CodeClass requestBuilder, CodeClass referencingRequestBuilder, LanguageWriter writer)
    {
        var requestBuilderNameAlias = string.Equals(requestBuilder.Name, referencingRequestBuilder.Name, StringComparison.OrdinalIgnoreCase) ? getCurrentRequestBuilderAlias(requestBuilder.Name) : requestBuilder.Name;
        var property = referencingRequestBuilder.Properties.FirstOrDefault(x => x.Kind == CodePropertyKind.RequestBuilder && x.Type is CodeType codeType && codeType.TypeDefinition == requestBuilder);
        writer.WriteLine($"Reflect.defineProperty({referencingRequestBuilder.Name.ToFirstCharacterUpperCase()}.prototype, \"{property.Name.ToFirstCharacterLowerCase()}\", {{");
        writer.IncreaseIndent();
        writer.WriteLine("configurable: true,");
        writer.WriteLine("enumerable: true,");
        writer.WriteLine($"get: function(this: {referencingRequestBuilder.Name.ToFirstCharacterUpperCase()}) {{");
        writer.IncreaseIndent();
        writer.WriteLine($"return new {requestBuilderNameAlias.ToFirstCharacterUpperCase()}(this.pathParameters,this.requestAdapter)");
        
        writer.WriteLine("}");
        writer.DecreaseIndent();
        writer.WriteLine("})");
    }
    private void WriteIndexerBody(CodeMethod codeElement, CodeClass parentClass, string returnType, LanguageWriter writer)
    {
        if (parentClass.GetPropertyOfKind(CodePropertyKind.PathParameters) is CodeProperty pathParametersProperty &&
            localConventions != null &&
            codeElement.OriginalIndexer != null)
        {
            localConventions.AddParametersAssignment(writer, pathParametersProperty.Type, $"this.{pathParametersProperty.Name}",
                (codeElement.OriginalIndexer.IndexType, codeElement.OriginalIndexer.SerializationName, "id"));
        }
    }
}
