using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers;
using Xunit;

namespace Kiota.Builder.Tests.Writers;

public class ProprietableBlockExtensions
{
    [Fact]
    public void GetsTheCodePathForFirstLevelProperty()
    {
        // Given
        var block = new CodeClass
        {
            Name = "testClass",
        };
        block.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                IsPrimaryErrorMessage = true,
                Type = new CodeType
                {
                    Name = "string",
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );

        // When
        var result = block.GetPrimaryMessageCodePath(
            static x => x.Name.ToFirstCharacterUpperCase(),
            static x => x.Name.ToFirstCharacterUpperCase(),
            "?."
        );

        // Then
        Assert.Equal("Prop1", result);
    }
    [Fact]
    public void GetsNothingOnNoPrimaryMessage()
    {
        // Given
        var block = new CodeClass
        {
            Name = "testClass",
        };
        block.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );

        // When
        var result = block.GetPrimaryMessageCodePath(
            static x => x.Name.ToFirstCharacterUpperCase(),
            static x => x.Name.ToFirstCharacterUpperCase(),
            "?."
        );

        // Then
        Assert.Empty(result);
    }
    [Fact]
    public void GetsTheCodePathForANestedProperty()
    {
        // Given
        var block = new CodeClass
        {
            Name = "testClass",
        };
        var nestedBlockLevel1 = new CodeClass
        {
            Name = "nestedClassLevel1",
        };
        var nestedBlockLevel2 = new CodeClass
        {
            Name = "nestedClassLevel2",
        };
        nestedBlockLevel2.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                IsPrimaryErrorMessage = true,
                Type = new CodeType
                {
                    Name = "string",
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );
        nestedBlockLevel1.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = nestedBlockLevel2.Name,
                    TypeDefinition = nestedBlockLevel2,
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );
        block.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = nestedBlockLevel1.Name,
                    TypeDefinition = nestedBlockLevel1,
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );

        // When
        var result = block.GetPrimaryMessageCodePath(
            static x => x.Name.ToFirstCharacterUpperCase(),
            static x => x.Name.ToFirstCharacterUpperCase(),
            "?."
        );

        // Then
        Assert.Equal("Prop1?.Prop1?.Prop1", result);
    }
    [Fact]
    public void GetsTheShortestCodePathForMultiplePrimaryMessages()
    {
        // Given
        var block = new CodeClass
        {
            Name = "testClass",
        };
        var nestedBlockLevel1 = new CodeClass
        {
            Name = "nestedClassLevel1",
        };
        var nestedBlockLevel2 = new CodeClass
        {
            Name = "nestedClassLevel2",
        };
        nestedBlockLevel2.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                IsPrimaryErrorMessage = true,
                Type = new CodeType
                {
                    Name = "string",
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );
        nestedBlockLevel1.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = nestedBlockLevel2.Name,
                    TypeDefinition = nestedBlockLevel2,
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                IsPrimaryErrorMessage = true,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );
        block.AddProperty(
            new CodeProperty
            {
                Name = "prop1",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = nestedBlockLevel1.Name,
                    TypeDefinition = nestedBlockLevel1,
                }
            },
            new CodeProperty
            {
                Name = "prop2",
                Kind = CodePropertyKind.Custom,
                Type = new CodeType
                {
                    Name = "string",
                }
            }
        );

        // When
        var result = block.GetPrimaryMessageCodePath(
            static x => x.Name.ToFirstCharacterUpperCase(),
            static x => x.Name.ToFirstCharacterUpperCase(),
            "?."
        );

        // Then
        Assert.Equal("Prop1?.Prop2", result);
    }
}
