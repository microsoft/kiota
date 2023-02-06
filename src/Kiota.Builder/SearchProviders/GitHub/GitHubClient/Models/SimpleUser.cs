using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    /// <summary>
    /// A GitHub user.
    /// </summary>
    public class SimpleUser : IAdditionalDataHolder, IParsable
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
        /// <summary>The email property</summary>
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
        /// <summary>The events_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? EventsUrl
        {
            get; set;
        }
#nullable restore
#else
        public string EventsUrl { get; set; }
#endif
        /// <summary>The followers_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? FollowersUrl
        {
            get; set;
        }
#nullable restore
#else
        public string FollowersUrl { get; set; }
#endif
        /// <summary>The following_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? FollowingUrl
        {
            get; set;
        }
#nullable restore
#else
        public string FollowingUrl { get; set; }
#endif
        /// <summary>The gists_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? GistsUrl
        {
            get; set;
        }
#nullable restore
#else
        public string GistsUrl { get; set; }
#endif
        /// <summary>The gravatar_id property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? GravatarId
        {
            get; set;
        }
#nullable restore
#else
        public string GravatarId { get; set; }
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
        /// <summary>The id property</summary>
        public int? Id
        {
            get; set;
        }
        /// <summary>The login property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Login
        {
            get; set;
        }
#nullable restore
#else
        public string Login { get; set; }
#endif
        /// <summary>The name property</summary>
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
        /// <summary>The organizations_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? OrganizationsUrl
        {
            get; set;
        }
#nullable restore
#else
        public string OrganizationsUrl { get; set; }
#endif
        /// <summary>The received_events_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? ReceivedEventsUrl
        {
            get; set;
        }
#nullable restore
#else
        public string ReceivedEventsUrl { get; set; }
#endif
        /// <summary>The repos_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? ReposUrl
        {
            get; set;
        }
#nullable restore
#else
        public string ReposUrl { get; set; }
#endif
        /// <summary>The site_admin property</summary>
        public bool? SiteAdmin
        {
            get; set;
        }
        /// <summary>The starred_at property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? StarredAt
        {
            get; set;
        }
#nullable restore
#else
        public string StarredAt { get; set; }
#endif
        /// <summary>The starred_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? StarredUrl
        {
            get; set;
        }
#nullable restore
#else
        public string StarredUrl { get; set; }
#endif
        /// <summary>The subscriptions_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? SubscriptionsUrl
        {
            get; set;
        }
#nullable restore
#else
        public string SubscriptionsUrl { get; set; }
#endif
        /// <summary>The type property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Type
        {
            get; set;
        }
#nullable restore
#else
        public string Type { get; set; }
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
        /// Instantiates a new SimpleUser and sets the default values.
        /// </summary>
        public SimpleUser()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static SimpleUser CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new SimpleUser();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"avatar_url", n => { AvatarUrl = n.GetStringValue(); } },
                {"email", n => { Email = n.GetStringValue(); } },
                {"events_url", n => { EventsUrl = n.GetStringValue(); } },
                {"followers_url", n => { FollowersUrl = n.GetStringValue(); } },
                {"following_url", n => { FollowingUrl = n.GetStringValue(); } },
                {"gists_url", n => { GistsUrl = n.GetStringValue(); } },
                {"gravatar_id", n => { GravatarId = n.GetStringValue(); } },
                {"html_url", n => { HtmlUrl = n.GetStringValue(); } },
                {"id", n => { Id = n.GetIntValue(); } },
                {"login", n => { Login = n.GetStringValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"node_id", n => { NodeId = n.GetStringValue(); } },
                {"organizations_url", n => { OrganizationsUrl = n.GetStringValue(); } },
                {"received_events_url", n => { ReceivedEventsUrl = n.GetStringValue(); } },
                {"repos_url", n => { ReposUrl = n.GetStringValue(); } },
                {"site_admin", n => { SiteAdmin = n.GetBoolValue(); } },
                {"starred_at", n => { StarredAt = n.GetStringValue(); } },
                {"starred_url", n => { StarredUrl = n.GetStringValue(); } },
                {"subscriptions_url", n => { SubscriptionsUrl = n.GetStringValue(); } },
                {"type", n => { Type = n.GetStringValue(); } },
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
            writer.WriteStringValue("avatar_url", AvatarUrl);
            writer.WriteStringValue("email", Email);
            writer.WriteStringValue("events_url", EventsUrl);
            writer.WriteStringValue("followers_url", FollowersUrl);
            writer.WriteStringValue("following_url", FollowingUrl);
            writer.WriteStringValue("gists_url", GistsUrl);
            writer.WriteStringValue("gravatar_id", GravatarId);
            writer.WriteStringValue("html_url", HtmlUrl);
            writer.WriteIntValue("id", Id);
            writer.WriteStringValue("login", Login);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("node_id", NodeId);
            writer.WriteStringValue("organizations_url", OrganizationsUrl);
            writer.WriteStringValue("received_events_url", ReceivedEventsUrl);
            writer.WriteStringValue("repos_url", ReposUrl);
            writer.WriteBoolValue("site_admin", SiteAdmin);
            writer.WriteStringValue("starred_at", StarredAt);
            writer.WriteStringValue("starred_url", StarredUrl);
            writer.WriteStringValue("subscriptions_url", SubscriptionsUrl);
            writer.WriteStringValue("type", Type);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
