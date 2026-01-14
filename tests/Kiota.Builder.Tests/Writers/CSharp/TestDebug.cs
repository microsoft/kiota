using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.CSharp;
using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp;

public class TestDebug
{
    [Fact]
    public void TestDoubleCollectionNullability()
    {
        var root = CodeNamespace.InitRootNamespace();
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, "./", "name");
        var tw = new StringWriter();
        writer.SetTextWriter(tw);
        
        var parentClass = new CodeClass { Name = "parentClass" };
        root.AddClass(parentClass);
        
        var unionTypeWrapper = root.AddClass(new CodeClass
        {
            Name = "UnionTypeWrapper",
            Kind = CodeClassKind.Model,
            OriginalComposedType = new CodeUnionType
            {
                Name = "UnionTypeWrapper",
            },
            DiscriminatorInformation = new()
            {
                DiscriminatorPropertyName = "@odata.type",
            },
        }).First();
        
        var cType1 = new CodeType
        {
            Name = "double",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
            // IsNullable NOT set - default is false
        };
        
        Console.WriteLine($"BEFORE adding to union - IsNullable: {cType1.IsNullable}");
        
        unionTypeWrapper.OriginalComposedType.AddType(cType1);
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "DoubleValue",
            Type = cType1,
            Kind = CodePropertyKind.Custom
        });
        
        Console.WriteLine($"AFTER adding to union - IsNullable: {cType1.IsNullable}");
        Console.WriteLine($"Property Type IsNullable: {unionTypeWrapper.GetPropertiesOfKind(CodePropertyKind.Custom).First().Type.IsNullable}");
        
        var factoryMethod = unionTypeWrapper.AddMethod(new CodeMethod
        {
            Name = "factory",
            Kind = CodeMethodKind.Factory,
            ReturnType = new CodeType
            {
                Name = "UnionTypeWrapper",
                TypeDefinition = unionTypeWrapper,
            },
        }).First();
        factoryMethod.AddParameter(new CodeParameter
        {
            Name = "parseNode",
            Kind = CodeParameterKind.ParseNode,
            Type = new CodeType
            {
                Name = "ParseNode"
            }
        });
        
        writer.Write(factoryMethod);
        var result = tw.ToString();
        Console.WriteLine("Generated code:");
        Console.WriteLine(result);
    }
}
