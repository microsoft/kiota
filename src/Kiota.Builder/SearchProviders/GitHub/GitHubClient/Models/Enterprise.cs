using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    /// <summary>
    /// An enterprise on GitHub.
    /// </summary>
    public class Enterprise : IAdditionalDataHolder, IParsable
    {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData
        {
            get; set;
        }
        /// <summary>The avatar_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? AvatarUrl
        {
            get; set;
        }
#nullable restore
#else
        public string AvatarUrl { get; set; }
#endif
        /// <summary>The created_at property</summary>
        public DateTimeOffset? CreatedAt
        {
            get; set;
        }
        /// <summary>A short description of the enterprise.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Description
        {
            get; set;
        }
#nullable restore
#else
        public string Description { get; set; }
#endif
        /// <summary>The html_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? HtmlUrl
        {
            get; set;
        }
#nullable restore
#else
        public string HtmlUrl { get; set; }
#endif
        /// <summary>Unique identifier of the enterprise</summary>
        public int? Id
        {
            get; set;
        }
        /// <summary>The name of the enterprise.</summary>
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
        /// <summary>The slug url identifier for the enterprise.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Slug
        {
            get; set;
        }
#nullable restore
#else
        public string Slug { get; set; }
#endif
        /// <summary>The updated_at property</summary>
        public DateTimeOffset? UpdatedAt
        {
            get; set;
        }
        /// <summary>The enterprise&apos;s website URL.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? WebsiteUrl
        {
            get; set;
        }
#nullable restore
#else
        public string WebsiteUrl { get; set; }
#endif
        /// <summary>
        /// Instantiates a new Enterprise and sets the default values.
        /// </summary>
        public Enterprise()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static Enterprise CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new Enterprise();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"avatar_url", n => { AvatarUrl = n.GetStringValue(); } },
                {"created_at", n => { CreatedAt = n.GetDateTimeOffsetValue(); } },
                {"description", n => { Description = n.GetStringValue(); } },
                {"html_url", n => { HtmlUrl = n.GetStringValue(); } },
                {"id", n => { Id = n.GetIntValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"node_id", n => { NodeId = n.GetStringValue(); } },
                {"slug", n => { Slug = n.GetStringValue(); } },
                {"updated_at", n => { UpdatedAt = n.GetDateTimeOffsetValue(); } },
                {"website_url", n => { WebsiteUrl = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("avatar_url", AvatarUrl);
            writer.WriteDateTimeOffsetValue("created_at", CreatedAt);
            writer.WriteStringValue("description", Description);
            writer.WriteStringValue("html_url", HtmlUrl);
            writer.WriteIntValue("id", Id);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("node_id", NodeId);
            writer.WriteStringValue("slug", Slug);
            writer.WriteDateTimeOffsetValue("updated_at", UpdatedAt);
            writer.WriteStringValue("website_url", WebsiteUrl);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
