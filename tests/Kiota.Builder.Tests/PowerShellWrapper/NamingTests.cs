using System.Collections.Generic;
using Kiota.Builder.PowerShellWrapper;
using Xunit;

namespace Kiota.Builder.Tests.PowerShellWrapper;

// The expected values are the published Microsoft.Graph cmdlet names, taken from the
// MgCommandMetadata.json inventory that ships inside Microsoft.Graph.Authentication.
// Changing one of these is a deliberate break from SDK parity and belongs in the
// migration guide, not in a quiet test edit.
public sealed class SingularizerTests
{
    [Theory]
    // ies -> y
    [InlineData("Policies", "Policy")]
    [InlineData("Categories", "Category")]
    // uses -> us (Get-MgDeviceManagementDeviceConfigurationUserStatus)
    [InlineData("Statuses", "Status")]
    // sibilant es
    [InlineData("Businesses", "Business")]
    [InlineData("Mailboxes", "Mailbox")]
    [InlineData("Branches", "Branch")]
    // ss/us/is guard
    [InlineData("Access", "Access")]
    [InlineData("Status", "Status")]
    [InlineData("Analysis", "Analysis")]
    // plain s
    [InlineData("Messages", "Message")]
    [InlineData("Plans", "Plan")]
    [InlineData("Settings", "Setting")]
    [InlineData("Licenses", "License")]
    // irregulars (Get-MgDriveItemChild, Get-MgUserPerson)
    [InlineData("Children", "Child")]
    [InlineData("People", "Person")]
    // invariants (Get-MgUserSettingWindows)
    [InlineData("Windows", "Windows")]
    // acronyms are never plural forms
    [InlineData("OS", "OS")]
    public void SingularizesWords(string word, string expected)
    {
        Assert.Equal(expected, Singularizer.SingularizeWord(word));
    }

    [Theory]
    [InlineData("Users", "User")]
    [InlineData("ManagedDevices", "ManagedDevice")]
    [InlineData("BookingBusinesses", "BookingBusiness")]
    [InlineData("ReportSettings", "ReportSetting")]
    [InlineData("ConditionalAccess", "ConditionalAccess")]
    // per-word inflection, matching the published SDK's inflector:
    // Update-MgDeviceManagementTermAndCondition
    [InlineData("TermsAndConditions", "TermAndCondition")]
    // Get-MgDirectoryOnPremiseSynchronization (interior word singularized)
    [InlineData("OnPremisesSynchronization", "OnPremiseSynchronization")]
    // version tag: Get-MgSecurityAlertV2
    [InlineData("Alerts_v2", "AlertV2")]
    public void SingularizesSegments(string segment, string expected)
    {
        Assert.Equal(expected, Singularizer.SingularizeSegment(segment));
    }
}

public sealed class NamingTests
{
    private static CmdletNaming Resolve(string method, string path)
    {
        var pathParams = new List<string>();
        foreach (var segment in path.Split('/'))
        {
            if (segment.StartsWith('{') && segment.EndsWith('}'))
                pathParams.Add(segment[1..^1]);
        }
        return Naming.Resolve(new OperationInfo(method, path, "test_op", pathParams, [], HasBody: false));
    }

    [Theory]
    // pilot module goldens (Microsoft.Graph.* 2.37.0 names)
    [InlineData("GET", "/users", "Get", "MgUser")]
    [InlineData("GET", "/users/{user-id}", "Get", "MgUser")]
    [InlineData("POST", "/users", "New", "MgUser")]
    [InlineData("PATCH", "/users/{user-id}", "Update", "MgUser")]
    [InlineData("DELETE", "/users/{user-id}", "Remove", "MgUser")]
    [InlineData("GET", "/users/{user-id}/messages", "Get", "MgUserMessage")]
    [InlineData("GET", "/users/{user-id}/messages/{message-id}", "Get", "MgUserMessage")]
    [InlineData("GET", "/users/{user-id}/contacts", "Get", "MgUserContact")]
    [InlineData("GET", "/applications/{application-id}", "Get", "MgApplication")]
    [InlineData("GET", "/deviceManagement/managedDevices", "Get", "MgDeviceManagementManagedDevice")]
    [InlineData("GET", "/identity/conditionalAccess/policies/{conditionalAccessPolicy-id}", "Get", "MgIdentityConditionalAccessPolicy")]
    [InlineData("GET", "/planner/plans", "Get", "MgPlannerPlan")]
    [InlineData("GET", "/security/alerts_v2", "Get", "MgSecurityAlertV2")]
    [InlineData("PATCH", "/admin/reportSettings", "Update", "MgAdminReportSetting")]
    [InlineData("GET", "/schemaExtensions", "Get", "MgSchemaExtension")]
    [InlineData("GET", "/domains/{domain-id}", "Get", "MgDomain")]
    [InlineData("GET", "/groups/{group-id}", "Get", "MgGroup")]
    [InlineData("GET", "/teams/{team-id}", "Get", "MgTeam")]
    // overrides mirroring the SDK's own AutoRest directives (see NamingOverrides)
    [InlineData("GET", "/solutions/bookingBusinesses/{bookingBusiness-id}", "Get", "MgBookingBusiness")]
    [InlineData("PATCH", "/solutions/bookingBusinesses/{bookingBusiness-id}", "Update", "MgBookingBusiness")]
    [InlineData("GET", "/users/{user-id}/calendar", "Get", "MgUserDefaultCalendar")]
    // boundary word-overlap collapse (Get-MgDomainNameReference)
    [InlineData("GET", "/domains/{domain-id}/domainNameReferences", "Get", "MgDomainNameReference")]
    // adjacent-duplicate collapse (Get-MgUserOnenoteSectionGroup... family)
    [InlineData("GET", "/users/{user-id}/onenote/sectionGroups/{sectionGroup-id}/sectionGroups", "Get", "MgUserOnenoteSectionGroup")]
    // OData cast segments (Get-MgGroupOwnerAsUser)
    [InlineData("GET", "/groups/{group-id}/owners/{directoryObject-id}/graph.user", "Get", "MgGroupOwnerAsUser")]
    public void ResolvesPublishedSdkNames(string method, string path, string expectedVerb, string expectedNoun)
    {
        var naming = Resolve(method, path);
        Assert.Equal(expectedVerb, naming.VerbName);
        Assert.Equal(expectedNoun, naming.Noun);
        Assert.Equal($"{expectedVerb}{expectedNoun}Command", naming.ClassName);
    }

    [Fact]
    public void SuppressesOperationsThePublishedSdkOmits()
    {
        // Calendar.md remove-path-by-operation user_UpdateCalendar: no Update cmdlet ships for
        // the default-calendar singleton.
        Assert.True(NamingOverrides.IsSuppressed("PATCH", "/users/{user-id}/calendar"));
        Assert.False(NamingOverrides.IsSuppressed("GET", "/users/{user-id}/calendar"));
        Assert.False(NamingOverrides.IsSuppressed("PATCH", "/users/{user-id}/messages/{message-id}"));
    }

    [Fact]
    public void ListAndItemGetsShareTheNounSoDispatcherPairingSurvives()
    {
        var list = Resolve("GET", "/users/{user-id}/messages");
        var item = Resolve("GET", "/users/{user-id}/messages/{message-id}");
        Assert.Equal(list.Noun, item.Noun);

        var internalList = Naming.WithSuffix(list, "_List");
        Assert.Equal("MgUserMessage_List", internalList.Noun);
        Assert.Equal("GetMgUserMessage_ListCommand", internalList.ClassName);
    }
}
