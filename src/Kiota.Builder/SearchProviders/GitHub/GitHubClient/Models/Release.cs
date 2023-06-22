using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>
    /// A release.
    /// </summary>
    public class Release : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The assets property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<ReleaseAsset>? Assets { get; set; }
#nullable restore
#else
        public List<ReleaseAsset> Assets { get; set; }
#endif
        /// <summary>The assets_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? AssetsUrl { get; set; }
#nullable restore
#else
        public string AssetsUrl { get; set; }
#endif
        /// <summary>A GitHub user.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public SimpleUser? Author { get; set; }
#nullable restore
#else
        public SimpleUser Author { get; set; }
#endif
        /// <summary>The body property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Body { get; set; }
#nullable restore
#else
        public string Body { get; set; }
#endif
        /// <summary>The body_html property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? BodyHtml { get; set; }
#nullable restore
#else
        public string BodyHtml { get; set; }
#endif
        /// <summary>The body_text property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? BodyText { get; set; }
#nullable restore
#else
        public string BodyText { get; set; }
#endif
        /// <summary>The created_at property</summary>
        public DateTimeOffset? CreatedAt { get; set; }
        /// <summary>The URL of the release discussion.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? DiscussionUrl { get; set; }
#nullable restore
#else
        public string DiscussionUrl { get; set; }
#endif
        /// <summary>true to create a draft (unpublished) release, false to create a published one.</summary>
        public bool? Draft { get; set; }
        /// <summary>The html_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? HtmlUrl { get; set; }
#nullable restore
#else
        public string HtmlUrl { get; set; }
#endif
        /// <summary>The id property</summary>
        public int? Id { get; set; }
        /// <summary>The mentions_count property</summary>
        public int? MentionsCount { get; set; }
        /// <summary>The name property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Name { get; set; }
#nullable restore
#else
        public string Name { get; set; }
#endif
        /// <summary>The node_id property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? NodeId { get; set; }
#nullable restore
#else
        public string NodeId { get; set; }
#endif
        /// <summary>Whether to identify the release as a prerelease or a full release.</summary>
        public bool? Prerelease { get; set; }
        /// <summary>The published_at property</summary>
        public DateTimeOffset? PublishedAt { get; set; }
        /// <summary>The reactions property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public ReactionRollup? Reactions { get; set; }
#nullable restore
#else
        public ReactionRollup Reactions { get; set; }
#endif
        /// <summary>The name of the tag.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? TagName { get; set; }
#nullable restore
#else
        public string TagName { get; set; }
#endif
        /// <summary>The tarball_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? TarballUrl { get; set; }
#nullable restore
#else
        public string TarballUrl { get; set; }
#endif
        /// <summary>Specifies the commitish value that determines where the Git tag is created from.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? TargetCommitish { get; set; }
#nullable restore
#else
        public string TargetCommitish { get; set; }
#endif
        /// <summary>The upload_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? UploadUrl { get; set; }
#nullable restore
#else
        public string UploadUrl { get; set; }
#endif
        /// <summary>The url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Url { get; set; }
#nullable restore
#else
        public string Url { get; set; }
#endif
        /// <summary>The zipball_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? ZipballUrl { get; set; }
#nullable restore
#else
        public string ZipballUrl { get; set; }
#endif
        /// <summary>
        /// Instantiates a new Release and sets the default values.
        /// </summary>
        public Release() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static Release CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new Release();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"assets", n => { Assets = n.GetCollectionOfObjectValues<ReleaseAsset>(ReleaseAsset.CreateFromDiscriminatorValue)?.ToList(); } },
                {"assets_url", n => { AssetsUrl = n.GetStringValue(); } },
                {"author", n => { Author = n.GetObjectValue<SimpleUser>(SimpleUser.CreateFromDiscriminatorValue); } },
                {"body", n => { Body = n.GetStringValue(); } },
                {"body_html", n => { BodyHtml = n.GetStringValue(); } },
                {"body_text", n => { BodyText = n.GetStringValue(); } },
                {"created_at", n => { CreatedAt = n.GetDateTimeOffsetValue(); } },
                {"discussion_url", n => { DiscussionUrl = n.GetStringValue(); } },
                {"draft", n => { Draft = n.GetBoolValue(); } },
                {"html_url", n => { HtmlUrl = n.GetStringValue(); } },
                {"id", n => { Id = n.GetIntValue(); } },
                {"mentions_count", n => { MentionsCount = n.GetIntValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"node_id", n => { NodeId = n.GetStringValue(); } },
                {"prerelease", n => { Prerelease = n.GetBoolValue(); } },
                {"published_at", n => { PublishedAt = n.GetDateTimeOffsetValue(); } },
                {"reactions", n => { Reactions = n.GetObjectValue<ReactionRollup>(ReactionRollup.CreateFromDiscriminatorValue); } },
                {"tag_name", n => { TagName = n.GetStringValue(); } },
                {"tarball_url", n => { TarballUrl = n.GetStringValue(); } },
                {"target_commitish", n => { TargetCommitish = n.GetStringValue(); } },
                {"upload_url", n => { UploadUrl = n.GetStringValue(); } },
                {"url", n => { Url = n.GetStringValue(); } },
                {"zipball_url", n => { ZipballUrl = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteCollectionOfObjectValues<ReleaseAsset>("assets", Assets);
            writer.WriteStringValue("assets_url", AssetsUrl);
            writer.WriteObjectValue<SimpleUser>("author", Author);
            writer.WriteStringValue("body", Body);
            writer.WriteStringValue("body_html", BodyHtml);
            writer.WriteStringValue("body_text", BodyText);
            writer.WriteDateTimeOffsetValue("created_at", CreatedAt);
            writer.WriteStringValue("discussion_url", DiscussionUrl);
            writer.WriteBoolValue("draft", Draft);
            writer.WriteStringValue("html_url", HtmlUrl);
            writer.WriteIntValue("id", Id);
            writer.WriteIntValue("mentions_count", MentionsCount);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("node_id", NodeId);
            writer.WriteBoolValue("prerelease", Prerelease);
            writer.WriteDateTimeOffsetValue("published_at", PublishedAt);
            writer.WriteObjectValue<ReactionRollup>("reactions", Reactions);
            writer.WriteStringValue("tag_name", TagName);
            writer.WriteStringValue("tarball_url", TarballUrl);
            writer.WriteStringValue("target_commitish", TargetCommitish);
            writer.WriteStringValue("upload_url", UploadUrl);
            writer.WriteStringValue("url", Url);
            writer.WriteStringValue("zipball_url", ZipballUrl);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
