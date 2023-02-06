using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    /// <summary>
    /// Installation
    /// </summary>
    public class Installation : IAdditionalDataHolder, IParsable
    {
        /// <summary>The access_tokens_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? AccessTokensUrl
        {
            get; set;
        }
#nullable restore
#else
        public string AccessTokensUrl { get; set; }
#endif
        /// <summary>The account property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public Installations? Account
        {
            get; set;
        }
#nullable restore
#else
        public Installations Account { get; set; }
#endif
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData
        {
            get; set;
        }
        /// <summary>The app_id property</summary>
        public int? AppId
        {
            get; set;
        }
        /// <summary>The app_slug property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? AppSlug
        {
            get; set;
        }
#nullable restore
#else
        public string AppSlug { get; set; }
#endif
        /// <summary>The contact_email property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? ContactEmail
        {
            get; set;
        }
#nullable restore
#else
        public string ContactEmail { get; set; }
#endif
        /// <summary>The created_at property</summary>
        public DateTimeOffset? CreatedAt
        {
            get; set;
        }
        /// <summary>The events property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<string>? Events
        {
            get; set;
        }
#nullable restore
#else
        public List<string> Events { get; set; }
#endif
        /// <summary>The has_multiple_single_files property</summary>
        public bool? HasMultipleSingleFiles
        {
            get; set;
        }
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
        /// <summary>The ID of the installation.</summary>
        public int? Id
        {
            get; set;
        }
        /// <summary>The permissions granted to the user-to-server access token.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public AppPermissions? Permissions
        {
            get; set;
        }
#nullable restore
#else
        public AppPermissions Permissions { get; set; }
#endif
        /// <summary>The repositories_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? RepositoriesUrl
        {
            get; set;
        }
#nullable restore
#else
        public string RepositoriesUrl { get; set; }
#endif
        /// <summary>Describe whether all repositories have been selected or there&apos;s a selection involved</summary>
        public Installation_repository_selection? RepositorySelection
        {
            get; set;
        }
        /// <summary>The single_file_name property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? SingleFileName
        {
            get; set;
        }
#nullable restore
#else
        public string SingleFileName { get; set; }
#endif
        /// <summary>The single_file_paths property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<string>? SingleFilePaths
        {
            get; set;
        }
#nullable restore
#else
        public List<string> SingleFilePaths { get; set; }
#endif
        /// <summary>The suspended_at property</summary>
        public DateTimeOffset? SuspendedAt
        {
            get; set;
        }
        /// <summary>A GitHub user.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public NullableSimpleUser? SuspendedBy
        {
            get; set;
        }
#nullable restore
#else
        public NullableSimpleUser SuspendedBy { get; set; }
#endif
        /// <summary>The ID of the user or organization this token is being scoped to.</summary>
        public int? TargetId
        {
            get; set;
        }
        /// <summary>The target_type property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? TargetType
        {
            get; set;
        }
#nullable restore
#else
        public string TargetType { get; set; }
#endif
        /// <summary>The updated_at property</summary>
        public DateTimeOffset? UpdatedAt
        {
            get; set;
        }
        /// <summary>
        /// Instantiates a new installation and sets the default values.
        /// </summary>
        public Installation()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static Installation CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new Installation();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"access_tokens_url", n => { AccessTokensUrl = n.GetStringValue(); } },
                {"account", n => { Account = n.GetObjectValue<Installations>(Installations.CreateFromDiscriminatorValue); } },
                {"app_id", n => { AppId = n.GetIntValue(); } },
                {"app_slug", n => { AppSlug = n.GetStringValue(); } },
                {"contact_email", n => { ContactEmail = n.GetStringValue(); } },
                {"created_at", n => { CreatedAt = n.GetDateTimeOffsetValue(); } },
                {"events", n => { Events = n.GetCollectionOfPrimitiveValues<string>()?.ToList(); } },
                {"has_multiple_single_files", n => { HasMultipleSingleFiles = n.GetBoolValue(); } },
                {"html_url", n => { HtmlUrl = n.GetStringValue(); } },
                {"id", n => { Id = n.GetIntValue(); } },
                {"permissions", n => { Permissions = n.GetObjectValue<AppPermissions>(AppPermissions.CreateFromDiscriminatorValue); } },
                {"repositories_url", n => { RepositoriesUrl = n.GetStringValue(); } },
                {"repository_selection", n => { RepositorySelection = n.GetEnumValue<Installation_repository_selection>(); } },
                {"single_file_name", n => { SingleFileName = n.GetStringValue(); } },
                {"single_file_paths", n => { SingleFilePaths = n.GetCollectionOfPrimitiveValues<string>()?.ToList(); } },
                {"suspended_at", n => { SuspendedAt = n.GetDateTimeOffsetValue(); } },
                {"suspended_by", n => { SuspendedBy = n.GetObjectValue<NullableSimpleUser>(NullableSimpleUser.CreateFromDiscriminatorValue); } },
                {"target_id", n => { TargetId = n.GetIntValue(); } },
                {"target_type", n => { TargetType = n.GetStringValue(); } },
                {"updated_at", n => { UpdatedAt = n.GetDateTimeOffsetValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("access_tokens_url", AccessTokensUrl);
            writer.WriteObjectValue<Installations>("account", Account);
            writer.WriteIntValue("app_id", AppId);
            writer.WriteStringValue("app_slug", AppSlug);
            writer.WriteStringValue("contact_email", ContactEmail);
            writer.WriteDateTimeOffsetValue("created_at", CreatedAt);
            writer.WriteCollectionOfPrimitiveValues<string>("events", Events);
            writer.WriteBoolValue("has_multiple_single_files", HasMultipleSingleFiles);
            writer.WriteStringValue("html_url", HtmlUrl);
            writer.WriteIntValue("id", Id);
            writer.WriteObjectValue<AppPermissions>("permissions", Permissions);
            writer.WriteStringValue("repositories_url", RepositoriesUrl);
            writer.WriteEnumValue<Installation_repository_selection>("repository_selection", RepositorySelection);
            writer.WriteStringValue("single_file_name", SingleFileName);
            writer.WriteCollectionOfPrimitiveValues<string>("single_file_paths", SingleFilePaths);
            writer.WriteDateTimeOffsetValue("suspended_at", SuspendedAt);
            writer.WriteObjectValue<NullableSimpleUser>("suspended_by", SuspendedBy);
            writer.WriteIntValue("target_id", TargetId);
            writer.WriteStringValue("target_type", TargetType);
            writer.WriteDateTimeOffsetValue("updated_at", UpdatedAt);
            writer.WriteAdditionalData(AdditionalData);
        }
        /// <summary>
        /// Composed type wrapper for classes simpleUser, enterprise
        /// </summary>
        public class Installations : IAdditionalDataHolder, IParsable
        {
            /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
            public IDictionary<string, object> AdditionalData
            {
                get; set;
            }
            /// <summary>Composed type representation for type enterprise</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
            public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.Enterprise? Enterprise
            {
                get; set;
            }
#nullable restore
#else
            public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.Enterprise Enterprise { get; set; }
#endif
            /// <summary>Serialization hint for the current wrapper.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
            public string? SerializationHint
            {
                get; set;
            }
#nullable restore
#else
            public string SerializationHint { get; set; }
#endif
            /// <summary>Composed type representation for type simpleUser</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
            public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.SimpleUser? SimpleUser
            {
                get; set;
            }
#nullable restore
#else
            public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.SimpleUser SimpleUser { get; set; }
#endif
            /// <summary>
            /// Instantiates a new installations and sets the default values.
            /// </summary>
            public Installations()
            {
                AdditionalData = new Dictionary<string, object>();
            }
            /// <summary>
            /// Creates a new instance of the appropriate class based on discriminator value
            /// </summary>
            /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
            public static Installations CreateFromDiscriminatorValue(IParseNode parseNode)
            {
                _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
                var result = new Installations();
                result.Enterprise = new Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.Enterprise();
                result.SimpleUser = new Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.SimpleUser();
                return result;
            }
            /// <summary>
            /// The deserialization information for the current model
            /// </summary>
            public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
            {
                if (Enterprise != null || SimpleUser != null)
                {
                    return ParseNodeHelper.MergeDeserializersForIntersectionWrapper(Enterprise, SimpleUser);
                }
                return new Dictionary<string, Action<IParseNode>>();
            }
            /// <summary>
            /// Serializes information the current object
            /// </summary>
            /// <param name="writer">Serialization writer to use to serialize this model</param>
            public void Serialize(ISerializationWriter writer)
            {
                _ = writer ?? throw new ArgumentNullException(nameof(writer));
                writer.WriteObjectValue<Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.Enterprise>(null, Enterprise, SimpleUser);
                writer.WriteAdditionalData(AdditionalData);
            }
        }
    }
}
