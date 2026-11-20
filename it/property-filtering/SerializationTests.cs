using System.Text.Json;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using PropertyFiltering.Client.Models;
using Xunit;

public sealed class SerializationTests
{
    [Fact]
    public async Task RetainsInheritedAndRecursiveFieldsThroughSerializationAsync()
    {
        using var payload = JsonDocument.Parse("""
            {"id":"base-id","kind":"widget","marker":"base-kept","name":"kept","extra":"preserved","choice":{"name":"optional"},
             "friends":[{"id":"friend-id","kind":"widget","name":"friend"}]}
            """);
        var parsed = new JsonParseNode(payload.RootElement).GetObjectValue(Base.CreateFromDiscriminatorValue);
        var widget = Assert.IsType<Widget>(parsed);
        Assert.Equal("base-id", widget.Id);
        Assert.Equal("kept", widget.Name);
        Assert.Equal("base-kept", widget.Marker);
        Assert.Equal("friend-id", Assert.Single(widget.Friends!).Id);
        Assert.Equal("optional", widget.Choice?.OptionalChoice?.Name);
        Assert.Null(typeof(Base).GetProperty("BaseOptional"));
        Assert.Null(typeof(Widget).GetProperty("Drop"));
        Assert.Null(typeof(Widget).GetProperty("Profile"));
        Assert.Null(typeof(Widget).Assembly.GetType("PropertyFiltering.Client.Models.Profile"));

        using var serialized = await SerializeAsync(widget);
        Assert.Equal("base-id", serialized.RootElement.GetProperty("id").GetString());
        Assert.Equal("widget", serialized.RootElement.GetProperty("kind").GetString());
        Assert.Equal("base-kept", serialized.RootElement.GetProperty("marker").GetString());
        Assert.Equal("kept", serialized.RootElement.GetProperty("name").GetString());
        Assert.Equal("friend", serialized.RootElement.GetProperty("friends")[0].GetProperty("name").GetString());
        Assert.Equal("preserved", serialized.RootElement.GetProperty("extra").GetString());
        Assert.Equal("optional", serialized.RootElement.GetProperty("choice").GetProperty("name").GetString());
        Assert.False(serialized.RootElement.GetProperty("choice").TryGetProperty("id", out _));
        Assert.False(serialized.RootElement.TryGetProperty("baseOptional", out _));
        Assert.False(serialized.RootElement.TryGetProperty("drop", out _));
        Assert.False(serialized.RootElement.TryGetProperty("profile", out _));
    }

    [Fact]
    public async Task RequiredUnionFieldsRemainLocalToTheirDeclarationAsync()
    {
        var required = new RequiredChoice { Id = "required", Keep = "kept" };
        var optional = new OptionalChoice { Name = "optional" };
        Assert.Contains("id", required.GetFieldDeserializers().Keys);
        Assert.DoesNotContain("id", optional.GetFieldDeserializers().Keys);
        Assert.Null(typeof(OptionalChoice).GetProperty("Id"));
        using var requiredJson = await SerializeAsync(required);
        using var optionalJson = await SerializeAsync(optional);
        Assert.Equal("required", requiredJson.RootElement.GetProperty("id").GetString());
        Assert.Equal("kept", requiredJson.RootElement.GetProperty("keep").GetString());
        Assert.Equal("optional", optionalJson.RootElement.GetProperty("name").GetString());
        Assert.False(optionalJson.RootElement.TryGetProperty("id", out _));
    }

    private static async Task<JsonDocument> SerializeAsync<T>(T model) where T : IParsable
    {
        using var writer = new JsonSerializationWriter();
        writer.WriteObjectValue(null, model);
        await using var stream = writer.GetSerializedContent();
        return await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
    }
}
