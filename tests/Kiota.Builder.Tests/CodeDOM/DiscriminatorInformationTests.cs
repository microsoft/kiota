using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Tests;
public class DiscriminatorInformationTests {

    [Fact]
    public void Defensive() {
        var information = new DiscriminatorInformation();
        Assert.Throws<ArgumentNullException>(() => information.AddDiscriminatorMapping(null, new CodeType()));
        Assert.Throws<ArgumentNullException>(() => information.AddDiscriminatorMapping("key", null));
        Assert.Throws<ArgumentNullException>(() => information.GetDiscriminatorMappingValue(null));
        Assert.Null(information.GetDiscriminatorMappingValue("key"));
        Assert.Empty(information.DiscriminatorMappings);
    }
    [Fact]
    public void AddsMapping() {
        var information = new DiscriminatorInformation();
        var type = new CodeType();
        information.AddDiscriminatorMapping("key", type);
        Assert.Equal(type, information.GetDiscriminatorMappingValue("key"));
        Assert.Single(information.DiscriminatorMappings);
    }
    [Fact]
    public void GetsMappingsInOrder() {
        var information = new DiscriminatorInformation();
        var type1 = new CodeType();
        var type2 = new CodeType();
        information.AddDiscriminatorMapping("key1", type1);
        information.AddDiscriminatorMapping("key2", type2);
        Assert.Equal(2, information.DiscriminatorMappings.Count());
        Assert.Equal(type1, information.GetDiscriminatorMappingValue("key1"));
        Assert.Equal(type2, information.GetDiscriminatorMappingValue("key2"));
        Assert.Equal(type1, information.DiscriminatorMappings.First().Value);
        Assert.Equal(type2, information.DiscriminatorMappings.Last().Value);
    }
    [Fact]
    public void Clones() {
        var information = new DiscriminatorInformation();
        var clone = information.Clone() as DiscriminatorInformation;
        Assert.NotNull(clone);
        Assert.NotEqual(information, clone);
    }
    [Fact]
    public void ShouldWriteDiscriminatorSwitch() {
        var information = new DiscriminatorInformation {
            Parent = new CodeMethod(),
        };
        Assert.False(information.ShouldWriteDiscriminatorSwitch);
        information.DiscriminatorPropertyName = "foo";
        Assert.False(information.ShouldWriteDiscriminatorSwitch);
        information.AddDiscriminatorMapping("key1", new CodeType());
        information.AddDiscriminatorMapping("key2", new CodeType());
        Assert.True(information.ShouldWriteDiscriminatorSwitch);

        information.Parent = new CodeUnionType();
        Assert.False(information.ShouldWriteDiscriminatorSwitch);
    }
    [Fact]
    public void ShouldWriteDiscriminatorForComposedType() {
        var information = new DiscriminatorInformation
        {
            Parent = new CodeUnionType()
        };
        Assert.False(information.ShouldWriteDiscriminatorForComposedType);
        information.DiscriminatorPropertyName = "foo";
        Assert.False(information.ShouldWriteDiscriminatorForComposedType);
        information.AddDiscriminatorMapping("key1", new CodeType());
        information.AddDiscriminatorMapping("key2", new CodeType());
        Assert.True(information.ShouldWriteDiscriminatorForComposedType);
        
        information.Parent = new CodeMethod();
        Assert.False(information.ShouldWriteDiscriminatorForComposedType);
    }
}
