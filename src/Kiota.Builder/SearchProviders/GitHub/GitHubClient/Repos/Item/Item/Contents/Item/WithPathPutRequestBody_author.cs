using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item.Contents.Item;
/// <summary>
/// The author of the file. Default: The `committer` or the authenticated user if you omit `committer`.
/// </summary>
public class WithPathPutRequestBody_author : IAdditionalDataHolder, IParsable
{
    /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
    public IDictionary<string, object> AdditionalData
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Date
    {
        get; set;
    }
#nullable restore
#else
    public string Date { get; set; }
#endif
    /// <summary>The email of the author or committer of the commit. You&apos;ll receive a `422` status code if `email` is omitted.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Email
    {
        get; set;
    }
#nullable restore
#else
    public string Email { get; set; }
#endif
    /// <summary>The name of the author or committer of the commit. You&apos;ll receive a `422` status code if `name` is omitted.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Name
    {
        get; set;
    }
#nullable restore
#else
    public string Name { get; set; }
#endif
    /// <summary>
    /// Instantiates a new WithPathPutRequestBody_author and sets the default values.
    /// </summary>
    public WithPathPutRequestBody_author()
    {
        AdditionalData = new Dictionary<string, object>();
    }
    /// <summary>
    /// Creates a new instance of the appropriate class based on discriminator value
    /// </summary>
    /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
    public static WithPathPutRequestBody_author CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
        return new WithPathPutRequestBody_author();
    }
    /// <summary>
    /// The deserialization information for the current model
    /// </summary>
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>> {
            {"date", n => { Date = n.GetStringValue(); } },
            {"email", n => { Email = n.GetStringValue(); } },
            {"name", n => { Name = n.GetStringValue(); } },
        };
    }
    /// <summary>
    /// Serializes information the current object
    /// </summary>
    /// <param name="writer">Serialization writer to use to serialize this model</param>
    public void Serialize(ISerializationWriter writer)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        writer.WriteStringValue("date", Date);
        writer.WriteStringValue("email", Email);
        writer.WriteStringValue("name", Name);
        writer.WriteAdditionalData(AdditionalData);
    }
}
