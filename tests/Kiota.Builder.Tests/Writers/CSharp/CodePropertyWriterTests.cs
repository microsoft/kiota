using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp;
public class CodePropertyWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeProperty property;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace rootNamespace;
    private const string PropertyName = "PropertyName";
    private const string TypeName = "Somecustomtype";
    public CodePropertyWriterTests()
    {
        writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.CSharp, DefaultPath, DefaultName);
        tw = new StringWriter();
        writer.SetTextWriter(tw);
        rootNamespace = CodeNamespace.InitRootNamespace().AddNamespace("defaultNamespace");
        parentClass = new CodeClass
        {
            Name = "parentClass"
        };
        var derivedClass = rootNamespace.AddClass(new CodeClass
        {
            Name = "SomeCustomClass"
        }).First();

        rootNamespace.AddClass(parentClass);
        property = new CodeProperty
        {
            Name = PropertyName,
            Type = new CodeType
            {
                Name = TypeName,
                TypeDefinition = derivedClass
            },
        };
        parentClass.AddProperty(property, new()
        {
            Name = "pathParameters",
            Kind = CodePropertyKind.PathParameters,
            Type = new CodeType
            {
                Name = "PathParameters",
            },
        }, new()
        {
            Name = "requestAdapter",
            Kind = CodePropertyKind.RequestAdapter,
            Type = new CodeType
            {
                Name = "RequestAdapter",
            },
        });
    }
    public void Dispose()
    {
        tw?.Dispose();
        GC.SuppressFinalize(this);
    }
    [Fact]
    public void WritesRequestBuilder()
    {
        property.Kind = CodePropertyKind.RequestBuilder;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("get =>", result);
        Assert.Contains($"new {TypeName}", result);
        Assert.Contains("RequestAdapter", result);
        Assert.Contains("PathParameters", result);
    }
    [Fact]
    public void WritesCustomProperty()
    {
        property.Kind = CodePropertyKind.Custom;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains($"{TypeName} {PropertyName}", result);
        Assert.Contains("get; set;", result);
    }
    [Fact]
    public void WritesPrivateSetter()
    {
        property.Kind = CodePropertyKind.Custom;
        property.ReadOnly = true;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("get; private set;", result);
    }
    [Fact]
    public void MapsCustomPropertiesToBackingStore()
    {
        parentClass.AddBackingStoreProperty();
        property.Kind = CodePropertyKind.Custom;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("get { return BackingStore?.Get<Somecustomtype>(\"propertyName\"); }", result);
        Assert.Contains("set { BackingStore?.Set(\"propertyName\", value);", result);
    }
    [Fact]
    public void MapsAdditionalDataPropertiesToBackingStore()
    {
        parentClass.AddBackingStoreProperty();
        property.Kind = CodePropertyKind.AdditionalData;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("get { return BackingStore?.Get<Somecustomtype>(\"propertyName\"); }", result);
        Assert.Contains("set { BackingStore?.Set(\"propertyName\", value);", result);
    }
    [Fact]
    public void WritesSerializationAttribute()
    {
        property.Kind = CodePropertyKind.QueryParameter;
        property.SerializationName = "someserializationname";
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("[QueryParameter(\"someserializationname\")", result);
    }
    [Fact]
    public void DoesntWritePropertiesExistingInParentType()
    {
        property.Kind = CodePropertyKind.Custom;
        property.Name = "definedInParent";
        var baseClass = (parentClass.Parent as CodeNamespace).AddClass(new CodeClass
        {
            Name = "BaseClass",
        }).First();
        parentClass.StartBlock.Inherits = new CodeType
        {
            Name = "BaseClass",
            TypeDefinition = baseClass
        };
        baseClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.Custom,
        });
        writer.Write(property);
        var result = tw.ToString();
        Assert.Empty(result);
    }

    [Fact]
    public void DisambiguateAmbiguousImportedTypes()
    {
        // Arrange : Adding a model with conflicting Types in properties from different namespaces.
        var defaultNamespace = rootNamespace.AddNamespace("models");
        var testModel = defaultNamespace.AddClass(
            new CodeClass
            {
                Name = "ModelWithPropertiesWithConflictingTypes"
            }).First();
        testModel.AddProperty(property);
        testModel.StartBlock.AddUsings(new CodeUsing
        {
            Name = defaultNamespace.Name,
            Declaration = property.Type as CodeType
        });

        var levelOneNameSpace = rootNamespace.AddNamespace("namespaceLevelOne");
        var anotherDerivedClass = levelOneNameSpace.AddClass(
            new CodeClass
            {
                Name = "SomeCustomClass"
            }).First();
        levelOneNameSpace.AddClass(anotherDerivedClass);
        var conflictingProperty = new CodeProperty
        {
            Name = $"{PropertyName}2",
            Type = new CodeType
            {
                Name = TypeName,
                TypeDefinition = anotherDerivedClass
            },
        };
        testModel.AddProperty(conflictingProperty);
        testModel.StartBlock.AddUsings(new CodeUsing
        {
            Name = levelOneNameSpace.Name,
            Declaration = conflictingProperty.Type as CodeType
        });

        // Act : Write the properties
        writer.Write(property);
        writer.Write(conflictingProperty);
        var result = tw.ToString();

        // Assert: properties types are disambiguated.
        Assert.Contains("namespaceLevelOne.Somecustomtype", result);
        Assert.Contains("defaultNamespace.Somecustomtype", result);
    }
    [Fact]
    public void WritesDeprecationInformation()
    {
        property.Deprecation = new("deprecation message", new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), "v1.0");
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("[Obsolete(\"deprecation message as of v1.0 on 2021-01-01 and will be removed 2023-01-01\")]", result);
    }
}

