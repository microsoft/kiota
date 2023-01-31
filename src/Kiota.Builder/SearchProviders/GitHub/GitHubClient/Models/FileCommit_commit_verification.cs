using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
public class FileCommit_commit_verification : IAdditionalDataHolder, IParsable
{
    /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
    public IDictionary<string, object> AdditionalData
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Payload
    {
        get; set;
    }
#nullable restore
#else
    public string Payload { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Reason
    {
        get; set;
    }
#nullable restore
#else
    public string Reason { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Signature
    {
        get; set;
    }
#nullable restore
#else
    public string Signature { get; set; }
#endif
    public bool? Verified
    {
        get; set;
    }
    /// <summary>
    /// Instantiates a new FileCommit_commit_verification and sets the default values.
    /// </summary>
    public FileCommit_commit_verification()
    {
        AdditionalData = new Dictionary<string, object>();
    }
    /// <summary>
    /// Creates a new instance of the appropriate class based on discriminator value
    /// </summary>
    /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
    public static FileCommit_commit_verification CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
        return new FileCommit_commit_verification();
    }
    /// <summary>
    /// The deserialization information for the current model
    /// </summary>
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>> {
            {"payload", n => { Payload = n.GetStringValue(); } },
            {"reason", n => { Reason = n.GetStringValue(); } },
            {"signature", n => { Signature = n.GetStringValue(); } },
            {"verified", n => { Verified = n.GetBoolValue(); } },
        };
    }
    /// <summary>
    /// Serializes information the current object
    /// </summary>
    /// <param name="writer">Serialization writer to use to serialize this model</param>
    public void Serialize(ISerializationWriter writer)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        writer.WriteStringValue("payload", Payload);
        writer.WriteStringValue("reason", Reason);
        writer.WriteStringValue("signature", Signature);
        writer.WriteBoolValue("verified", Verified);
        writer.WriteAdditionalData(AdditionalData);
    }
}
