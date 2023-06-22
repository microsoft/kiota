using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    public class FileCommit_commit : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The author property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public FileCommit_commit_author? Author { get; set; }
#nullable restore
#else
        public FileCommit_commit_author Author { get; set; }
#endif
        /// <summary>The committer property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public FileCommit_commit_committer? Committer { get; set; }
#nullable restore
#else
        public FileCommit_commit_committer Committer { get; set; }
#endif
        /// <summary>The html_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? HtmlUrl { get; set; }
#nullable restore
#else
        public string HtmlUrl { get; set; }
#endif
        /// <summary>The message property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Message { get; set; }
#nullable restore
#else
        public string Message { get; set; }
#endif
        /// <summary>The node_id property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? NodeId { get; set; }
#nullable restore
#else
        public string NodeId { get; set; }
#endif
        /// <summary>The parents property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<FileCommit_commit_parents>? Parents { get; set; }
#nullable restore
#else
        public List<FileCommit_commit_parents> Parents { get; set; }
#endif
        /// <summary>The sha property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Sha { get; set; }
#nullable restore
#else
        public string Sha { get; set; }
#endif
        /// <summary>The tree property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public FileCommit_commit_tree? Tree { get; set; }
#nullable restore
#else
        public FileCommit_commit_tree Tree { get; set; }
#endif
        /// <summary>The url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Url { get; set; }
#nullable restore
#else
        public string Url { get; set; }
#endif
        /// <summary>The verification property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public FileCommit_commit_verification? Verification { get; set; }
#nullable restore
#else
        public FileCommit_commit_verification Verification { get; set; }
#endif
        /// <summary>
        /// Instantiates a new FileCommit_commit and sets the default values.
        /// </summary>
        public FileCommit_commit() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static FileCommit_commit CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new FileCommit_commit();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"author", n => { Author = n.GetObjectValue<FileCommit_commit_author>(FileCommit_commit_author.CreateFromDiscriminatorValue); } },
                {"committer", n => { Committer = n.GetObjectValue<FileCommit_commit_committer>(FileCommit_commit_committer.CreateFromDiscriminatorValue); } },
                {"html_url", n => { HtmlUrl = n.GetStringValue(); } },
                {"message", n => { Message = n.GetStringValue(); } },
                {"node_id", n => { NodeId = n.GetStringValue(); } },
                {"parents", n => { Parents = n.GetCollectionOfObjectValues<FileCommit_commit_parents>(FileCommit_commit_parents.CreateFromDiscriminatorValue)?.ToList(); } },
                {"sha", n => { Sha = n.GetStringValue(); } },
                {"tree", n => { Tree = n.GetObjectValue<FileCommit_commit_tree>(FileCommit_commit_tree.CreateFromDiscriminatorValue); } },
                {"url", n => { Url = n.GetStringValue(); } },
                {"verification", n => { Verification = n.GetObjectValue<FileCommit_commit_verification>(FileCommit_commit_verification.CreateFromDiscriminatorValue); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteObjectValue<FileCommit_commit_author>("author", Author);
            writer.WriteObjectValue<FileCommit_commit_committer>("committer", Committer);
            writer.WriteStringValue("html_url", HtmlUrl);
            writer.WriteStringValue("message", Message);
            writer.WriteStringValue("node_id", NodeId);
            writer.WriteCollectionOfObjectValues<FileCommit_commit_parents>("parents", Parents);
            writer.WriteStringValue("sha", Sha);
            writer.WriteObjectValue<FileCommit_commit_tree>("tree", Tree);
            writer.WriteStringValue("url", Url);
            writer.WriteObjectValue<FileCommit_commit_verification>("verification", Verification);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
