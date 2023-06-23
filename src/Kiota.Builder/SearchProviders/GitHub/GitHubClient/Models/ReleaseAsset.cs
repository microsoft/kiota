using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    /// <summary>
    /// Data related to a release.
    /// </summary>
    public class ReleaseAsset : IAdditionalDataHolder, IParsable
    {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData
        {
            get; set;
        }
        /// <summary>The browser_download_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? BrowserDownloadUrl
        {
            get; set;
        }
#nullable restore
#else
        public string BrowserDownloadUrl { get; set; }
#endif
        /// <summary>The content_type property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? ContentType
        {
            get; set;
        }
#nullable restore
#else
        public string ContentType { get; set; }
#endif
        /// <summary>The created_at property</summary>
        public DateTimeOffset? CreatedAt
        {
            get; set;
        }
        /// <summary>The download_count property</summary>
        public int? DownloadCount
        {
            get; set;
        }
        /// <summary>The id property</summary>
        public int? Id
        {
            get; set;
        }
        /// <summary>The label property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Label
        {
            get; set;
        }
#nullable restore
#else
        public string Label { get; set; }
#endif
        /// <summary>The file name of the asset.</summary>
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
        /// <summary>The node_id property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? NodeId
        {
            get; set;
        }
#nullable restore
#else
        public string NodeId { get; set; }
#endif
        /// <summary>The size property</summary>
        public int? Size
        {
            get; set;
        }
        /// <summary>State of the release asset.</summary>
        public ReleaseAsset_state? State
        {
            get; set;
        }
        /// <summary>The updated_at property</summary>
        public DateTimeOffset? UpdatedAt
        {
            get; set;
        }
        /// <summary>A GitHub user.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public NullableSimpleUser? Uploader
        {
            get; set;
        }
#nullable restore
#else
        public NullableSimpleUser Uploader { get; set; }
#endif
        /// <summary>The url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Url
        {
            get; set;
        }
#nullable restore
#else
        public string Url { get; set; }
#endif
        /// <summary>
        /// Instantiates a new releaseAsset and sets the default values.
        /// </summary>
        public ReleaseAsset()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static ReleaseAsset CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new ReleaseAsset();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"browser_download_url", n => { BrowserDownloadUrl = n.GetStringValue(); } },
                {"content_type", n => { ContentType = n.GetStringValue(); } },
                {"created_at", n => { CreatedAt = n.GetDateTimeOffsetValue(); } },
                {"download_count", n => { DownloadCount = n.GetIntValue(); } },
                {"id", n => { Id = n.GetIntValue(); } },
                {"label", n => { Label = n.GetStringValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"node_id", n => { NodeId = n.GetStringValue(); } },
                {"size", n => { Size = n.GetIntValue(); } },
                {"state", n => { State = n.GetEnumValue<ReleaseAsset_state>(); } },
                {"updated_at", n => { UpdatedAt = n.GetDateTimeOffsetValue(); } },
                {"uploader", n => { Uploader = n.GetObjectValue<NullableSimpleUser>(NullableSimpleUser.CreateFromDiscriminatorValue); } },
                {"url", n => { Url = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("browser_download_url", BrowserDownloadUrl);
            writer.WriteStringValue("content_type", ContentType);
            writer.WriteDateTimeOffsetValue("created_at", CreatedAt);
            writer.WriteIntValue("download_count", DownloadCount);
            writer.WriteIntValue("id", Id);
            writer.WriteStringValue("label", Label);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("node_id", NodeId);
            writer.WriteIntValue("size", Size);
            writer.WriteEnumValue<ReleaseAsset_state>("state", State);
            writer.WriteDateTimeOffsetValue("updated_at", UpdatedAt);
            writer.WriteObjectValue<NullableSimpleUser>("uploader", Uploader);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
