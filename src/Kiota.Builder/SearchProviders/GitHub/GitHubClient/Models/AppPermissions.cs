using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
/// <summary>
/// The permissions granted to the user-to-server access token.
/// </summary>
public class AppPermissions : IAdditionalDataHolder, IParsable {
    /// <summary>The level of permission to grant the access token for GitHub Actions workflows, workflow runs, and artifacts.</summary>
    public AppPermissions_actions? Actions { get; set; }
    /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
    public IDictionary<string, object> AdditionalData { get; set; }
    /// <summary>The level of permission to grant the access token for repository creation, deletion, settings, teams, and collaborators creation.</summary>
    public AppPermissions_administration? Administration { get; set; }
    /// <summary>The level of permission to grant the access token for checks on code.</summary>
    public AppPermissions_checks? Checks { get; set; }
    /// <summary>The level of permission to grant the access token for repository contents, commits, branches, downloads, releases, and merges.</summary>
    public AppPermissions_contents? Contents { get; set; }
    /// <summary>The level of permission to grant the access token for deployments and deployment statuses.</summary>
    public AppPermissions_deployments? Deployments { get; set; }
    /// <summary>The level of permission to grant the access token for managing repository environments.</summary>
    public AppPermissions_environments? Environments { get; set; }
    /// <summary>The level of permission to grant the access token for issues and related comments, assignees, labels, and milestones.</summary>
    public AppPermissions_issues? Issues { get; set; }
    /// <summary>The level of permission to grant the access token for organization teams and members.</summary>
    public AppPermissions_members? Members { get; set; }
    /// <summary>The level of permission to grant the access token to search repositories, list collaborators, and access repository metadata.</summary>
    public AppPermissions_metadata? Metadata { get; set; }
    /// <summary>The level of permission to grant the access token to manage access to an organization.</summary>
    public AppPermissions_organization_administration? Organization_administration { get; set; }
    /// <summary>The level of permission to grant the access token to view and manage announcement banners for an organization.</summary>
    public AppPermissions_organization_announcement_banners? Organization_announcement_banners { get; set; }
    /// <summary>The level of permission to grant the access token for custom repository roles management. This property is in beta and is subject to change.</summary>
    public AppPermissions_organization_custom_roles? Organization_custom_roles { get; set; }
    /// <summary>The level of permission to grant the access token to manage the post-receive hooks for an organization.</summary>
    public AppPermissions_organization_hooks? Organization_hooks { get; set; }
    /// <summary>The level of permission to grant the access token for organization packages published to GitHub Packages.</summary>
    public AppPermissions_organization_packages? Organization_packages { get; set; }
    /// <summary>The level of permission to grant the access token for viewing an organization&apos;s plan.</summary>
    public AppPermissions_organization_plan? Organization_plan { get; set; }
    /// <summary>The level of permission to grant the access token to manage organization projects and projects beta (where available).</summary>
    public AppPermissions_organization_projects? Organization_projects { get; set; }
    /// <summary>The level of permission to grant the access token to manage organization secrets.</summary>
    public AppPermissions_organization_secrets? Organization_secrets { get; set; }
    /// <summary>The level of permission to grant the access token to view and manage GitHub Actions self-hosted runners available to an organization.</summary>
    public AppPermissions_organization_self_hosted_runners? Organization_self_hosted_runners { get; set; }
    /// <summary>The level of permission to grant the access token to view and manage users blocked by the organization.</summary>
    public AppPermissions_organization_user_blocking? Organization_user_blocking { get; set; }
    /// <summary>The level of permission to grant the access token for packages published to GitHub Packages.</summary>
    public AppPermissions_packages? Packages { get; set; }
    /// <summary>The level of permission to grant the access token to retrieve Pages statuses, configuration, and builds, as well as create new builds.</summary>
    public AppPermissions_pages? Pages { get; set; }
    /// <summary>The level of permission to grant the access token for pull requests and related comments, assignees, labels, milestones, and merges.</summary>
    public AppPermissions_pull_requests? Pull_requests { get; set; }
    /// <summary>The level of permission to grant the access token to view and manage announcement banners for a repository.</summary>
    public AppPermissions_repository_announcement_banners? Repository_announcement_banners { get; set; }
    /// <summary>The level of permission to grant the access token to manage the post-receive hooks for a repository.</summary>
    public AppPermissions_repository_hooks? Repository_hooks { get; set; }
    /// <summary>The level of permission to grant the access token to manage repository projects, columns, and cards.</summary>
    public AppPermissions_repository_projects? Repository_projects { get; set; }
    /// <summary>The level of permission to grant the access token to view and manage secret scanning alerts.</summary>
    public AppPermissions_secret_scanning_alerts? Secret_scanning_alerts { get; set; }
    /// <summary>The level of permission to grant the access token to manage repository secrets.</summary>
    public AppPermissions_secrets? Secrets { get; set; }
    /// <summary>The level of permission to grant the access token to view and manage security events like code scanning alerts.</summary>
    public AppPermissions_security_events? Security_events { get; set; }
    /// <summary>The level of permission to grant the access token to manage just a single file.</summary>
    public AppPermissions_single_file? Single_file { get; set; }
    /// <summary>The level of permission to grant the access token for commit statuses.</summary>
    public AppPermissions_statuses? Statuses { get; set; }
    /// <summary>The level of permission to grant the access token to manage team discussions and related comments.</summary>
    public AppPermissions_team_discussions? Team_discussions { get; set; }
    /// <summary>The level of permission to grant the access token to manage Dependabot alerts.</summary>
    public AppPermissions_vulnerability_alerts? Vulnerability_alerts { get; set; }
    /// <summary>The level of permission to grant the access token to update GitHub Actions workflow files.</summary>
    public AppPermissions_workflows? Workflows { get; set; }
    /// <summary>
    /// Instantiates a new appPermissions and sets the default values.
    /// </summary>
    public AppPermissions() {
        AdditionalData = new Dictionary<string, object>();
    }
    /// <summary>
    /// Creates a new instance of the appropriate class based on discriminator value
    /// </summary>
    /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
    public static AppPermissions CreateFromDiscriminatorValue(IParseNode parseNode) {
        _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
        return new AppPermissions();
    }
    /// <summary>
    /// The deserialization information for the current model
    /// </summary>
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
        return new Dictionary<string, Action<IParseNode>> {
            {"actions", n => { Actions = n.GetEnumValue<AppPermissions_actions>(); } },
            {"administration", n => { Administration = n.GetEnumValue<AppPermissions_administration>(); } },
            {"checks", n => { Checks = n.GetEnumValue<AppPermissions_checks>(); } },
            {"contents", n => { Contents = n.GetEnumValue<AppPermissions_contents>(); } },
            {"deployments", n => { Deployments = n.GetEnumValue<AppPermissions_deployments>(); } },
            {"environments", n => { Environments = n.GetEnumValue<AppPermissions_environments>(); } },
            {"issues", n => { Issues = n.GetEnumValue<AppPermissions_issues>(); } },
            {"members", n => { Members = n.GetEnumValue<AppPermissions_members>(); } },
            {"metadata", n => { Metadata = n.GetEnumValue<AppPermissions_metadata>(); } },
            {"organization_administration", n => { Organization_administration = n.GetEnumValue<AppPermissions_organization_administration>(); } },
            {"organization_announcement_banners", n => { Organization_announcement_banners = n.GetEnumValue<AppPermissions_organization_announcement_banners>(); } },
            {"organization_custom_roles", n => { Organization_custom_roles = n.GetEnumValue<AppPermissions_organization_custom_roles>(); } },
            {"organization_hooks", n => { Organization_hooks = n.GetEnumValue<AppPermissions_organization_hooks>(); } },
            {"organization_packages", n => { Organization_packages = n.GetEnumValue<AppPermissions_organization_packages>(); } },
            {"organization_plan", n => { Organization_plan = n.GetEnumValue<AppPermissions_organization_plan>(); } },
            {"organization_projects", n => { Organization_projects = n.GetEnumValue<AppPermissions_organization_projects>(); } },
            {"organization_secrets", n => { Organization_secrets = n.GetEnumValue<AppPermissions_organization_secrets>(); } },
            {"organization_self_hosted_runners", n => { Organization_self_hosted_runners = n.GetEnumValue<AppPermissions_organization_self_hosted_runners>(); } },
            {"organization_user_blocking", n => { Organization_user_blocking = n.GetEnumValue<AppPermissions_organization_user_blocking>(); } },
            {"packages", n => { Packages = n.GetEnumValue<AppPermissions_packages>(); } },
            {"pages", n => { Pages = n.GetEnumValue<AppPermissions_pages>(); } },
            {"pull_requests", n => { Pull_requests = n.GetEnumValue<AppPermissions_pull_requests>(); } },
            {"repository_announcement_banners", n => { Repository_announcement_banners = n.GetEnumValue<AppPermissions_repository_announcement_banners>(); } },
            {"repository_hooks", n => { Repository_hooks = n.GetEnumValue<AppPermissions_repository_hooks>(); } },
            {"repository_projects", n => { Repository_projects = n.GetEnumValue<AppPermissions_repository_projects>(); } },
            {"secret_scanning_alerts", n => { Secret_scanning_alerts = n.GetEnumValue<AppPermissions_secret_scanning_alerts>(); } },
            {"secrets", n => { Secrets = n.GetEnumValue<AppPermissions_secrets>(); } },
            {"security_events", n => { Security_events = n.GetEnumValue<AppPermissions_security_events>(); } },
            {"single_file", n => { Single_file = n.GetEnumValue<AppPermissions_single_file>(); } },
            {"statuses", n => { Statuses = n.GetEnumValue<AppPermissions_statuses>(); } },
            {"team_discussions", n => { Team_discussions = n.GetEnumValue<AppPermissions_team_discussions>(); } },
            {"vulnerability_alerts", n => { Vulnerability_alerts = n.GetEnumValue<AppPermissions_vulnerability_alerts>(); } },
            {"workflows", n => { Workflows = n.GetEnumValue<AppPermissions_workflows>(); } },
        };
    }
    /// <summary>
    /// Serializes information the current object
    /// </summary>
    /// <param name="writer">Serialization writer to use to serialize this model</param>
    public void Serialize(ISerializationWriter writer) {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        writer.WriteEnumValue<AppPermissions_actions>("actions", Actions);
        writer.WriteEnumValue<AppPermissions_administration>("administration", Administration);
        writer.WriteEnumValue<AppPermissions_checks>("checks", Checks);
        writer.WriteEnumValue<AppPermissions_contents>("contents", Contents);
        writer.WriteEnumValue<AppPermissions_deployments>("deployments", Deployments);
        writer.WriteEnumValue<AppPermissions_environments>("environments", Environments);
        writer.WriteEnumValue<AppPermissions_issues>("issues", Issues);
        writer.WriteEnumValue<AppPermissions_members>("members", Members);
        writer.WriteEnumValue<AppPermissions_metadata>("metadata", Metadata);
        writer.WriteEnumValue<AppPermissions_organization_administration>("organization_administration", Organization_administration);
        writer.WriteEnumValue<AppPermissions_organization_announcement_banners>("organization_announcement_banners", Organization_announcement_banners);
        writer.WriteEnumValue<AppPermissions_organization_custom_roles>("organization_custom_roles", Organization_custom_roles);
        writer.WriteEnumValue<AppPermissions_organization_hooks>("organization_hooks", Organization_hooks);
        writer.WriteEnumValue<AppPermissions_organization_packages>("organization_packages", Organization_packages);
        writer.WriteEnumValue<AppPermissions_organization_plan>("organization_plan", Organization_plan);
        writer.WriteEnumValue<AppPermissions_organization_projects>("organization_projects", Organization_projects);
        writer.WriteEnumValue<AppPermissions_organization_secrets>("organization_secrets", Organization_secrets);
        writer.WriteEnumValue<AppPermissions_organization_self_hosted_runners>("organization_self_hosted_runners", Organization_self_hosted_runners);
        writer.WriteEnumValue<AppPermissions_organization_user_blocking>("organization_user_blocking", Organization_user_blocking);
        writer.WriteEnumValue<AppPermissions_packages>("packages", Packages);
        writer.WriteEnumValue<AppPermissions_pages>("pages", Pages);
        writer.WriteEnumValue<AppPermissions_pull_requests>("pull_requests", Pull_requests);
        writer.WriteEnumValue<AppPermissions_repository_announcement_banners>("repository_announcement_banners", Repository_announcement_banners);
        writer.WriteEnumValue<AppPermissions_repository_hooks>("repository_hooks", Repository_hooks);
        writer.WriteEnumValue<AppPermissions_repository_projects>("repository_projects", Repository_projects);
        writer.WriteEnumValue<AppPermissions_secret_scanning_alerts>("secret_scanning_alerts", Secret_scanning_alerts);
        writer.WriteEnumValue<AppPermissions_secrets>("secrets", Secrets);
        writer.WriteEnumValue<AppPermissions_security_events>("security_events", Security_events);
        writer.WriteEnumValue<AppPermissions_single_file>("single_file", Single_file);
        writer.WriteEnumValue<AppPermissions_statuses>("statuses", Statuses);
        writer.WriteEnumValue<AppPermissions_team_discussions>("team_discussions", Team_discussions);
        writer.WriteEnumValue<AppPermissions_vulnerability_alerts>("vulnerability_alerts", Vulnerability_alerts);
        writer.WriteEnumValue<AppPermissions_workflows>("workflows", Workflows);
        writer.WriteAdditionalData(AdditionalData);
    }
}
