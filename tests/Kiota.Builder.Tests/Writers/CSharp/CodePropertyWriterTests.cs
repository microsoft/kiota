using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers;

using Xunit;

namespace Kiota.Builder.Tests.Writers.CSharp;

public sealed class CodePropertyWriterTests : IDisposable
{
    private const string DefaultPath = "./";
    private const string DefaultName = "name";
    private readonly StringWriter tw;
    private readonly LanguageWriter writer;
    private readonly CodeProperty property;
    private readonly CodeClass parentClass;
    private readonly CodeNamespace rootNamespace;
    private const string PropertyName = "PropertyName";
    private const string TypeName = "SomeCustomClass";
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
            SerializationName = "propertyName",
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
        Assert.Contains($"new global::{rootNamespace.Name}.{TypeName}", result);
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
        Assert.Contains("get { return BackingStore?.Get<global::" + rootNamespace.Name + ".SomeCustomClass>(\"propertyName\"); }", result);
        Assert.Contains("set { BackingStore?.Set(\"propertyName\", value);", result);
    }
    [Fact]
    public void MapsAdditionalDataPropertiesToBackingStore()
    {
        parentClass.AddBackingStoreProperty();
        property.Kind = CodePropertyKind.AdditionalData;
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("get { return BackingStore.Get<global::" + rootNamespace.Name + ".SomeCustomClass>(\"propertyName\") ?? new Dictionary<string, object>(); }", result);
        Assert.Contains("set { BackingStore.Set(\"propertyName\", value);", result);
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
        parentClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.Custom,
        });
        var subClass = (parentClass.Parent as CodeNamespace).AddClass(new CodeClass
        {
            Name = "BaseClass",
        }).First();
        subClass.StartBlock.Inherits = new CodeType
        {
            Name = "BaseClass",
            TypeDefinition = parentClass
        };
        var propertyToWrite = subClass.AddProperty(new CodeProperty
        {
            Name = "definedInParent",
            Type = new CodeType
            {
                Name = "string"
            },
            Kind = CodePropertyKind.Custom,
        }).First();
        writer.Write(propertyToWrite);
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
        Assert.Contains("namespaceLevelOne.SomeCustomClass", result);
        Assert.Contains($"{rootNamespace.Name}.SomeCustomClass", result);
    }
    [Fact]
    public void WritesDeprecationInformation()
    {
        property.Deprecation = new("deprecation message", new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), "v1.0");
        writer.Write(property);
        var result = tw.ToString();
        Assert.Contains("[Obsolete(\"deprecation message as of v1.0 on 2021-01-01 and will be removed 2023-01-01\")]", result);
    }
    [Fact]
    public void WritesMessageOverrideOnPrimary()
    {
        // Given
        parentClass.IsErrorDefinition = true;
        parentClass.AddProperty(new CodeProperty
        {
            Name = "prop1",
            Kind = CodePropertyKind.Custom,
            IsPrimaryErrorMessage = true,
            Type = new CodeType
            {
                Name = "string",
            },
        });
        var overrideProperty = parentClass.AddProperty(new CodeProperty
        {
            Name = "Message",
            Kind = CodePropertyKind.ErrorMessageOverride,
            Type = new CodeType
            {
                Name = "string",
            },
        }).First();

        // When
        writer.Write(overrideProperty);
        var result = tw.ToString();

        // Then
        Assert.Contains("public override string Message { get => Prop1 ?? string.Empty; }", result);
    }
}

