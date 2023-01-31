using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item.Contents.Item;
public class WithPathDeleteRequestBody : IAdditionalDataHolder, IParsable
{
    /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
    public IDictionary<string, object> AdditionalData
    {
        get; set;
    }
    /// <summary>object containing information about the author.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public WithPathDeleteRequestBody_author? Author
    {
        get; set;
    }
#nullable restore
#else
    public WithPathDeleteRequestBody_author Author { get; set; }
#endif
    /// <summary>The branch name. Default: the repository’s default branch (usually `master`)</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Branch
    {
        get; set;
    }
#nullable restore
#else
    public string Branch { get; set; }
#endif
    /// <summary>object containing information about the committer.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public WithPathDeleteRequestBody_committer? Committer
    {
        get; set;
    }
#nullable restore
#else
    public WithPathDeleteRequestBody_committer Committer { get; set; }
#endif
    /// <summary>The commit message.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Message
    {
        get; set;
    }
#nullable restore
#else
    public string Message { get; set; }
#endif
    /// <summary>The blob SHA of the file being deleted.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Sha
    {
        get; set;
    }
#nullable restore
#else
    public string Sha { get; set; }
#endif
    /// <summary>
    /// Instantiates a new WithPathDeleteRequestBody and sets the default values.
    /// </summary>
    public WithPathDeleteRequestBody()
    {
        AdditionalData = new Dictionary<string, object>();
    }
    /// <summary>
    /// Creates a new instance of the appropriate class based on discriminator value
    /// </summary>
    /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
    public static WithPathDeleteRequestBody CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
        return new WithPathDeleteRequestBody();
    }
    /// <summary>
    /// The deserialization information for the current model
    /// </summary>
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>> {
            {"author", n => { Author = n.GetObjectValue<WithPathDeleteRequestBody_author>(WithPathDeleteRequestBody_author.CreateFromDiscriminatorValue); } },
            {"branch", n => { Branch = n.GetStringValue(); } },
            {"committer", n => { Committer = n.GetObjectValue<WithPathDeleteRequestBody_committer>(WithPathDeleteRequestBody_committer.CreateFromDiscriminatorValue); } },
            {"message", n => { Message = n.GetStringValue(); } },
            {"sha", n => { Sha = n.GetStringValue(); } },
        };
    }
    /// <summary>
    /// Serializes information the current object
    /// </summary>
    /// <param name="writer">Serialization writer to use to serialize this model</param>
    public void Serialize(ISerializationWriter writer)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        writer.WriteObjectValue<WithPathDeleteRequestBody_author>("author", Author);
        writer.WriteStringValue("branch", Branch);
        writer.WriteObjectValue<WithPathDeleteRequestBody_committer>("committer", Committer);
        writer.WriteStringValue("message", Message);
        writer.WriteStringValue("sha", Sha);
        writer.WriteAdditionalData(AdditionalData);
    }
}
