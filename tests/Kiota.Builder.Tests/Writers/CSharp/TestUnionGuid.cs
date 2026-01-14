using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.CSharp;
using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp;

public class TestUnionGuid
{
    [Fact]
    public void TestGuidCollection()
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
            Name = "Guid",
            CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Complex,
            IsNullable = true, // Make the inner type nullable
        };
        
        unionTypeWrapper.OriginalComposedType.AddType(cType1);
        unionTypeWrapper.AddProperty(new CodeProperty
        {
            Name = "GuidValue",
            Type = cType1,
            Kind = CodePropertyKind.Custom
        });
        
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
        Console.WriteLine("---");
        
        // Check what we expect
        var expectedPattern = "parseNode.GetCollectionOfPrimitiveValues<Guid?>()?.AsList() is List<Guid?> guidValue";
        Console.WriteLine($"Looking for: {expectedPattern}");
        Console.WriteLine($"Found: {result.Contains(expectedPattern)}");
        
        Assert.Contains(expectedPattern, result);
    }
}
