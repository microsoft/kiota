using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Repos.Item.Item.Releases
{
    public class ReleasesPostRequestBody : IAdditionalDataHolder, IParsable
    {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData
        {
            get; set;
        }
        /// <summary>Text describing the contents of the tag.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Body
        {
            get; set;
        }
#nullable restore
#else
        public string Body { get; set; }
#endif
        /// <summary>If specified, a discussion of the specified category is created and linked to the release. The value must be a category that already exists in the repository. For more information, see &quot;[Managing categories for discussions in your repository](https://docs.github.com/discussions/managing-discussions-for-your-community/managing-categories-for-discussions-in-your-repository).&quot;</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? DiscussionCategoryName
        {
            get; set;
        }
#nullable restore
#else
        public string DiscussionCategoryName { get; set; }
#endif
        /// <summary>`true` to create a draft (unpublished) release, `false` to create a published one.</summary>
        public bool? Draft
        {
            get; set;
        }
        /// <summary>Whether to automatically generate the name and body for this release. If `name` is specified, the specified name will be used; otherwise, a name will be automatically generated. If `body` is specified, the body will be pre-pended to the automatically generated notes.</summary>
        public bool? GenerateReleaseNotes
        {
            get; set;
        }
        /// <summary>Specifies whether this release should be set as the latest release for the repository. Drafts and prereleases cannot be set as latest. Defaults to `true` for newly published releases. `legacy` specifies that the latest release should be determined based on the release creation date and higher semantic version.</summary>
        public ReleasesPostRequestBody_make_latest? MakeLatest
        {
            get; set;
        }
        /// <summary>The name of the release.</summary>
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
        /// <summary>`true` to identify the release as a prerelease. `false` to identify the release as a full release.</summary>
        public bool? Prerelease
        {
            get; set;
        }
        /// <summary>The name of the tag.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? TagName
        {
            get; set;
        }
#nullable restore
#else
        public string TagName { get; set; }
#endif
        /// <summary>Specifies the commitish value that determines where the Git tag is created from. Can be any branch or commit SHA. Unused if the Git tag already exists. Default: the repository&apos;s default branch.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? TargetCommitish
        {
            get; set;
        }
#nullable restore
#else
        public string TargetCommitish { get; set; }
#endif
        /// <summary>
        /// Instantiates a new releasesPostRequestBody and sets the default values.
        /// </summary>
        public ReleasesPostRequestBody()
        {
            AdditionalData = new Dictionary<string, object>();
            MakeLatest = ReleasesPostRequestBody_make_latest.True;
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static ReleasesPostRequestBody CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new ReleasesPostRequestBody();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"body", n => { Body = n.GetStringValue(); } },
                {"discussion_category_name", n => { DiscussionCategoryName = n.GetStringValue(); } },
                {"draft", n => { Draft = n.GetBoolValue(); } },
                {"generate_release_notes", n => { GenerateReleaseNotes = n.GetBoolValue(); } },
                {"make_latest", n => { MakeLatest = n.GetEnumValue<ReleasesPostRequestBody_make_latest>(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"prerelease", n => { Prerelease = n.GetBoolValue(); } },
                {"tag_name", n => { TagName = n.GetStringValue(); } },
                {"target_commitish", n => { TargetCommitish = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("body", Body);
            writer.WriteStringValue("discussion_category_name", DiscussionCategoryName);
            writer.WriteBoolValue("draft", Draft);
            writer.WriteBoolValue("generate_release_notes", GenerateReleaseNotes);
            writer.WriteEnumValue<ReleasesPostRequestBody_make_latest>("make_latest", MakeLatest);
            writer.WriteStringValue("name", Name);
            writer.WriteBoolValue("prerelease", Prerelease);
            writer.WriteStringValue("tag_name", TagName);
            writer.WriteStringValue("target_commitish", TargetCommitish);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
