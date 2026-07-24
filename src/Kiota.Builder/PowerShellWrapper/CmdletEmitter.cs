using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.PowerShellWrapper;

// Emits the wrapper cmdlet shapes from section 8 of the design spec, one cmdlet class per
// selected OpenAPI operation: mandatory path-id parameters, an optional -AccessToken with a
// Connect-MgGraph fallback, a ShouldProcess gate on mutating calls, and the Kiota client's
// property/indexer chain for the actual request.
public static class CmdletEmitter
{
    // Repeated verbatim in every emitted cmdlet. EmitSharedAuth provides the two helpers this
    // block depends on: IsParameterBound and StaticBearerTokenAuthenticationProvider.
    private const string AuthBlock = """

                // ── Choose HttpClient + auth provider ─────────────────────────────
                HttpClient httpClient;
                IAuthenticationProvider authProvider;

                if (this.IsParameterBound(nameof(AccessToken)))
                {
                    httpClient = new HttpClient();
                    authProvider = new StaticBearerTokenAuthenticationProvider(AccessToken!);
                }
                else
                {
                    WriteVerbose("[MgPoC] No -AccessToken supplied, using the active Connect-MgGraph session.");
                    try
                    {
                        httpClient = HttpHelpers.GetGraphHttpClient();
                    }
                    catch (Exception ex)
                    {
                        ThrowTerminatingError(new ErrorRecord(
                            new InvalidOperationException(
                                "No active Graph session. Run Connect-MgGraph first, or supply -AccessToken.", ex),
                            "NoGraphSession",
                            ErrorCategory.AuthenticationError,
                            null));
                        return;
                    }
                    authProvider = new AnonymousAuthenticationProvider();
                }

                var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
                var client = new ApiClient(requestAdapter);
        """;

    private static string PathParams(CmdletNaming naming) =>
        string.Join("\n", naming.PathParamNames.Select((name, i) => $$"""
                [Parameter(Mandatory = true, Position = {{i}})]
                public string {{name}} { get; set; } = string.Empty;
        """));

    private static string TargetId(CmdletNaming naming) =>
        naming.PathParamNames.Count > 0 ? naming.PathParamNames[^1] : "null";

    // Header parameters declared in the spec (most commonly an "If-Match" ETag on PATCH/DELETE)
    // become real cmdlet parameters. Graph sometimes requires them even when the spec marks
    // them optional; Planner's PATCH/DELETE is the known example. Dropping them would make
    // those endpoints impossible to call.
    private static string HeaderParamDecls(CmdletNaming naming) => HeaderParamDeclsFor(naming.HeaderParams, parameterSetName: null);

    private static string HeaderParamDeclsFor(IReadOnlyList<HeaderParam> headers, string? parameterSetName)
    {
        var setAttr = parameterSetName is null ? "" : $", ParameterSetName = \"{parameterSetName}\"";
        return string.Join("", headers.Select(h => $$"""


                [Parameter(Mandatory = false{{setAttr}},
                    HelpMessage = "Sets the '{{h.RawName}}' request header (for example an ETag for optimistic concurrency; some Graph APIs require it even where the spec marks it optional).")]
                public string? {{h.PsName}} { get; set; }
        """));
    }

    private static string HeaderBindings(CmdletNaming naming) => HeaderBindingsFor(naming.HeaderParams, extraIndent: "");

    // extraIndent shifts the lines 4 spaces deeper for call sites inside nested if/else blocks.
    private static string HeaderBindingsFor(IReadOnlyList<HeaderParam> headers, string extraIndent) =>
        string.Join("", headers.Select(h => $$"""


                {{extraIndent}}if (this.IsParameterBound(nameof({{h.PsName}})))
                {{extraIndent}}    requestConfiguration.Headers.Add("{{h.RawName}}", {{h.PsName}}!);
        """));

    // Splits a paired list/item GET's header parameters into: declared on both operations (bind
    // regardless of which parameter set is active), list-only, and get-only.
    private static (IReadOnlyList<HeaderParam> Shared, IReadOnlyList<HeaderParam> ListOnly, IReadOnlyList<HeaderParam> GetOnly) PartitionHeaderParams(
        CmdletNaming listNaming, CmdletNaming itemNaming)
    {
        var listNames = listNaming.HeaderParams.Select(h => h.RawName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var itemNames = itemNaming.HeaderParams.Select(h => h.RawName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var shared = listNaming.HeaderParams.Where(h => itemNames.Contains(h.RawName)).ToList();
        var listOnly = listNaming.HeaderParams.Where(h => !itemNames.Contains(h.RawName)).ToList();
        var getOnly = itemNaming.HeaderParams.Where(h => !listNames.Contains(h.RawName)).ToList();
        return (shared, listOnly, getOnly);
    }

    // Every emitted cmdlet accepts -AccessToken as an alternative to an active Connect-MgGraph
    // session. See AuthBlock.
    private static string AccessTokenParamDecl() => """
        [Parameter(Mandatory = false,
            HelpMessage = "Bearer access token. Omit if you have already run Connect-MgGraph.")]
        public string? AccessToken { get; set; }
""";

    // The shared try/catch tail around every Graph call. Only the ErrorRecord's target object
    // varies, and sometimes the nesting depth (EmitUpdate's re-fetch sits one block deeper).
    private static string CatchBlock(string targetIdExpr, string extraIndent = "") => $$"""
            {{extraIndent}}catch (Exception ex)
            {{extraIndent}}{
                {{extraIndent}}ThrowTerminatingError(new ErrorRecord(ex, "GraphRequestFailed", ErrorCategory.InvalidOperation, {{targetIdExpr}}));
                {{extraIndent}}return;
            {{extraIndent}}}
""";

    // The -Headers dictionary, matching the published SDK's parameter of the same name. It
    // lets a caller set any header, not just the ones the spec declares.
    private static string GenericHeadersParamDecl() => $$"""


                [Parameter(Mandatory = false,
                    HelpMessage = "Additional HTTP request headers to send, keyed by header name.")]
                public System.Collections.IDictionary? Headers { get; set; }
        """;

    private static string GenericHeadersBinding(string extraIndent = "") => $$"""


                {{extraIndent}}if (this.IsParameterBound(nameof(Headers)))
                {{extraIndent}}{
                {{extraIndent}}    foreach (System.Collections.DictionaryEntry entry in Headers!)
                {{extraIndent}}        requestConfiguration.Headers.Add(entry.Key.ToString()!, entry.Value?.ToString() ?? string.Empty);
                {{extraIndent}}}
        """;

    // Post/Patch/DeleteAsync always take a requestConfiguration lambda, because -Headers exists
    // on every cmdlet. Reuses the same binding fragments the GET emitters use, at this call
    // site's deeper indent, so there is one binding implementation instead of three.
    private static string EmitCallWithOptionalHeaders(CmdletNaming naming, string method, string? bodyArg)
    {
        var call = $"client.{naming.BuilderExpression}.{method}(";
        var args = bodyArg is null ? "" : bodyArg + ", ";
        var bindings = HeaderBindingsFor(naming.HeaderParams, extraIndent: "                ") + GenericHeadersBinding("                ");
        return $"{call}{args}requestConfiguration =>\n                {{{bindings}\n                }})";
    }

    public static string EmitItemGet(CmdletNaming naming, EmitContext ctx, string entityType)
    {
        ArgumentNullException.ThrowIfNull(naming);
        ArgumentNullException.ThrowIfNull(ctx);
        return $$"""
#nullable enable

using System;
using System.Management.Automation;
using System.Net.Http;
using Microsoft.Graph.PowerShell.Authentication.Helpers;
using {{ctx.ClientNamespace}};
using {{ctx.ModelsNamespace}};
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace {{ctx.CmdletNamespace}}
{
    [Cmdlet(VerbsCommon.{{naming.VerbName}}, "{{naming.Noun}}")]
    [OutputType(typeof({{entityType}}))]
    public class {{naming.ClassName}} : PSCmdlet
    {
{{PathParams(naming)}}

{{AccessTokenParamDecl()}}

        [Parameter(Mandatory = false)]
        [Alias("Select")]
        public string[]? Property { get; set; }

        [Parameter(Mandatory = false)]
        [Alias("Expand")]
        public string[]? ExpandProperty { get; set; }
{{HeaderParamDecls(naming)}}
{{GenericHeadersParamDecl()}}

        protected override void ProcessRecord()
        {
{{AuthBlock}}

            {{entityType}} result;
            try
            {
                result = client.{{naming.BuilderExpression}}.GetAsync(requestConfiguration =>
                {
                    if (this.IsParameterBound(nameof(Property)))
                        requestConfiguration.QueryParameters.Select = Property;

                    if (this.IsParameterBound(nameof(ExpandProperty)))
                        requestConfiguration.QueryParameters.Expand = ExpandProperty;
{{HeaderBindings(naming)}}
{{GenericHeadersBinding()}}
                }).GetAwaiter().GetResult();
            }
{{CatchBlock(TargetId(naming))}}

            WriteObject(result);
        }
    }
}

""";
    }

    // Collection GETs do not all support the same OData query options (GET /users has no $skip,
    // for example), so the emitted parameters follow what the operation declares, not a fixed
    // set. Primary names match the published SDK (Property, ExpandProperty, Sort); the
    // wrapper's original names stay as aliases so -Select/-Expand/-OrderBy keep working.
    //
    // ParamDecl takes the owning parameter-set name (null when the cmdlet has no sets) so the
    // dispatcher can derive its "List"-tagged declarations from this same table instead of
    // keeping a second copy that would drift.
    private static string ParamSetDecl(string? parameterSetName, string propertyDecl, string? alias = null)
    {
        var setAttr = parameterSetName is null ? "" : $", ParameterSetName = \"{parameterSetName}\"";
        var aliasLine = alias is null ? "" : $"\n        [Alias(\"{alias}\")]";
        return $"        [Parameter(Mandatory = false{setAttr})]{aliasLine}\n        {propertyDecl}";
    }

    private static readonly (string ODataName, string PsName, Func<string?, string> ParamDecl, string Binding)[] CollectionQueryOptions =
    [
        ("$filter", "Filter", ps => ParamSetDecl(ps, "public string? Filter { get; set; }"),
            "                    if (this.IsParameterBound(nameof(Filter)))\n                        requestConfiguration.QueryParameters.Filter = Filter;"),
        ("$select", "Property", ps => ParamSetDecl(ps, "public string[]? Property { get; set; }", alias: "Select"),
            "                    if (this.IsParameterBound(nameof(Property)))\n                        requestConfiguration.QueryParameters.Select = Property;"),
        ("$expand", "ExpandProperty", ps => ParamSetDecl(ps, "public string[]? ExpandProperty { get; set; }", alias: "Expand"),
            "                    if (this.IsParameterBound(nameof(ExpandProperty)))\n                        requestConfiguration.QueryParameters.Expand = ExpandProperty;"),
        ("$orderby", "Sort", ps => ParamSetDecl(ps, "public string[]? Sort { get; set; }", alias: "OrderBy"),
            "                    if (this.IsParameterBound(nameof(Sort)))\n                        requestConfiguration.QueryParameters.Orderby = Sort;"),
        ("$search", "Search", ps => ParamSetDecl(ps, "public string? Search { get; set; }"),
            "                    if (this.IsParameterBound(nameof(Search)))\n                        requestConfiguration.QueryParameters.Search = Search;"),
        ("$top", "Top", ps => ParamSetDecl(ps, "public int Top { get; set; }"),
            "                    if (this.IsParameterBound(nameof(Top)))\n                        requestConfiguration.QueryParameters.Top = Top;"),
        ("$skip", "Skip", ps => ParamSetDecl(ps, "public int Skip { get; set; }"),
            "                    if (this.IsParameterBound(nameof(Skip)))\n                        requestConfiguration.QueryParameters.Skip = Skip;"),
        ("$count", "Count", ps => ParamSetDecl(ps, "public SwitchParameter Count { get; set; }"),
            "                    if (Count.IsPresent)\n                        requestConfiguration.QueryParameters.Count = true;"),
    ];

    public static string EmitListGet(CmdletNaming naming, EmitContext ctx, string entityType, string collectionResponseType, IReadOnlySet<string> queryParamNames)
    {
        ArgumentNullException.ThrowIfNull(naming);
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(queryParamNames);
        var applicable = CollectionQueryOptions.Where(o => queryParamNames.Contains(o.ODataName)).ToList();
        var paramDecls = string.Join("\n\n", applicable.Select(o => o.ParamDecl(null)));
        var bindings = string.Join("\n\n", applicable.Select(o => o.Binding));

        return $$"""
#nullable enable

using System;
using System.Management.Automation;
using System.Net.Http;
using Microsoft.Graph.PowerShell.Authentication.Helpers;
using {{ctx.ClientNamespace}};
using {{ctx.ModelsNamespace}};
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace {{ctx.CmdletNamespace}}
{
    [Cmdlet(VerbsCommon.{{naming.VerbName}}, "{{naming.Noun}}")]
    [OutputType(typeof({{entityType}}))]
    public class {{naming.ClassName}} : PSCmdlet
    {
{{PathParams(naming)}}

{{AccessTokenParamDecl()}}

{{paramDecls}}
{{HeaderParamDecls(naming)}}
{{GenericHeadersParamDecl()}}

        protected override void ProcessRecord()
        {
{{AuthBlock}}

            {{collectionResponseType}} result;
            try
            {
                result = client.{{naming.BuilderExpression}}.GetAsync(requestConfiguration =>
                {
{{bindings}}
{{HeaderBindings(naming)}}
{{GenericHeadersBinding()}}
                }).GetAwaiter().GetResult();
            }
{{CatchBlock(TargetId(naming))}}

            WriteObject(result.Value, enumerateCollection: true);
        }
    }
}

""";
    }

    // The dispatcher's list-only parameter declarations: CollectionQueryOptions minus
    // $select/$expand, which are shared with the "Get" set and declared once at class level.
    // Declarations only; binding happens in the internal list cmdlet the dispatcher calls.
    private static IEnumerable<(string ODataName, string ParamDecl)> ListOnlyQueryOptionsForMerge() =>
        CollectionQueryOptions
            .Where(o => o.ODataName is not ("$select" or "$expand"))
            .Select(o => (o.ODataName, o.ParamDecl("List")));

    // Shared path params (-UserId on a nested list) carry no ParameterSetName, which PowerShell
    // treats as "all sets". The trailing item id (-MessageId) belongs to the "Get" set only and
    // continues the Position sequence where the shared ones left off.
    private static string PairedPathParams(IReadOnlyList<string> sharedNames, IReadOnlyList<string> getOnlyNames)
    {
        var parts = new List<string>();
        var sharedDecls = string.Join("\n", sharedNames.Select((name, i) => $$"""
                [Parameter(Mandatory = true, Position = {{i}})]
                public string {{name}} { get; set; } = string.Empty;
        """));
        if (sharedDecls.Length > 0)
            parts.Add(sharedDecls);

        var getOnlyDecls = string.Join("\n", getOnlyNames.Select((name, i) => $$"""
                [Parameter(Mandatory = true, ParameterSetName = "Get", Position = {{sharedNames.Count + i}})]
                public string {{name}} { get; set; } = string.Empty;
        """));
        if (getOnlyDecls.Length > 0)
            parts.Add(getOnlyDecls);

        return string.Join("\n", parts);
    }

    // The thin public cmdlet for a paired list/item GET. It presents the merged Get-MgX surface
    // the published SDK exposes ("List" as the default set, "Get" for item lookups) but makes no
    // HTTP call itself. Per the design spec's parameter-set decision, the real work stays in the
    // two internal cmdlets; ProcessRecord only picks one and forwards the bound parameters. The
    // forward goes through InvokeCommand.InvokeScript on the current runspace, so the nested
    // call shares the caller's session, including an active Connect-MgGraph.
    public static string EmitGetDispatcher(CmdletNaming listNaming, CmdletNaming itemNaming,
        CmdletNaming internalListNaming, CmdletNaming internalItemNaming, EmitContext ctx,
        string entityType, string collectionResponseType, IReadOnlySet<string> queryParamNames)
    {
        ArgumentNullException.ThrowIfNull(listNaming);
        ArgumentNullException.ThrowIfNull(itemNaming);
        ArgumentNullException.ThrowIfNull(internalListNaming);
        ArgumentNullException.ThrowIfNull(internalItemNaming);
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(queryParamNames);

        var sharedPathParams = listNaming.PathParamNames;
        var getOnlyPathParams = itemNaming.PathParamNames.Skip(sharedPathParams.Count).ToList();

        var applicable = ListOnlyQueryOptionsForMerge().Where(o => queryParamNames.Contains(o.ODataName)).ToList();
        var listOnlyParamDecls = string.Join("\n\n", applicable.Select(o => o.ParamDecl));

        var (sharedHeaders, listOnlyHeaders, getOnlyHeaders) = PartitionHeaderParams(listNaming, itemNaming);

        var internalListCmdletName = $"{internalListNaming.VerbName}-{internalListNaming.Noun}";
        var internalGetCmdletName = $"{internalItemNaming.VerbName}-{internalItemNaming.Noun}";

        return $$"""
#nullable enable

using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using {{ctx.ModelsNamespace}};

namespace {{ctx.CmdletNamespace}}
{
    [Cmdlet(VerbsCommon.Get, "{{listNaming.Noun}}", DefaultParameterSetName = "List")]
    [OutputType(typeof({{collectionResponseType}}), ParameterSetName = new[] { "List" })]
    [OutputType(typeof({{entityType}}), ParameterSetName = new[] { "Get" })]
    public class {{listNaming.ClassName}} : PSCmdlet
    {
{{PairedPathParams(sharedPathParams, getOnlyPathParams)}}

{{AccessTokenParamDecl()}}

        [Parameter(Mandatory = false)]
        [Alias("Select")]
        public string[]? Property { get; set; }

        [Parameter(Mandatory = false)]
        [Alias("Expand")]
        public string[]? ExpandProperty { get; set; }

{{listOnlyParamDecls}}
{{HeaderParamDeclsFor(sharedHeaders, parameterSetName: null)}}
{{HeaderParamDeclsFor(listOnlyHeaders, parameterSetName: "List")}}
{{HeaderParamDeclsFor(getOnlyHeaders, parameterSetName: "Get")}}
{{GenericHeadersParamDecl()}}

        // Delegates to {{internalGetCmdletName}} or {{internalListCmdletName}}, the two cmdlets
        // that actually call Graph.
        protected override void ProcessRecord()
        {
            var internalCmdletName = ParameterSetName == "Get" ? "{{internalGetCmdletName}}" : "{{internalListCmdletName}}";
            try
            {
                InvokeCommand.InvokeScript(
                    "param($BoundParameters, $CmdletName) & $CmdletName @BoundParameters",
                    false,
                    PipelineResultTypes.Output | PipelineResultTypes.Error,
                    null,
                    MyInvocation.BoundParameters, internalCmdletName);
            }
{{CatchBlock(TargetId(itemNaming))}}
        }
    }
}

""";
    }

    public static string EmitNew(CmdletNaming naming, EmitContext ctx, string entityType, IReadOnlyList<CmdletProperty> properties, bool hasPasswordProfile)
    {
        ArgumentNullException.ThrowIfNull(naming);
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(properties);
        return $$"""
#nullable enable

using System;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using Microsoft.Graph.PowerShell.Authentication.Helpers;
using {{ctx.ClientNamespace}};
using {{ctx.ModelsNamespace}};
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace {{ctx.CmdletNamespace}}
{
    [Cmdlet(VerbsCommon.{{naming.VerbName}}, "{{naming.Noun}}", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof({{entityType}}))]
    public class {{naming.ClassName}} : PSCmdlet
    {
{{PathParams(naming)}}
{{EmitPropertyParameters(properties)}}
{{(hasPasswordProfile ? EmitPasswordProfileParameters() : "")}}
{{HeaderParamDecls(naming)}}
{{GenericHeadersParamDecl()}}

{{AccessTokenParamDecl()}}

        protected override void ProcessRecord()
        {
            if (!ShouldProcess({{TargetId(naming)}}, "{{naming.VerbName}}"))
                return;

            var body = new {{entityType}}();
{{EmitPropertyAssignments(properties)}}
{{(hasPasswordProfile ? EmitPasswordProfileAssignment() : "")}}
{{AuthBlock}}

            {{entityType}}? result;
            try
            {
                result = {{EmitCallWithOptionalHeaders(naming, "PostAsync", "body")}}.GetAwaiter().GetResult();
            }
{{CatchBlock("body")}}

            WriteObject(result);
        }
    }
}

""";
    }

    public static string EmitUpdate(CmdletNaming naming, EmitContext ctx, string entityType, IReadOnlyList<CmdletProperty> properties, bool hasPasswordProfile)
    {
        ArgumentNullException.ThrowIfNull(naming);
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(properties);
        return $$"""
#nullable enable

using System;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using Microsoft.Graph.PowerShell.Authentication.Helpers;
using {{ctx.ClientNamespace}};
using {{ctx.ModelsNamespace}};
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace {{ctx.CmdletNamespace}}
{
    [Cmdlet(VerbsData.{{naming.VerbName}}, "{{naming.Noun}}", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof({{entityType}}))]
    public class {{naming.ClassName}} : PSCmdlet
    {
{{PathParams(naming)}}
{{EmitPropertyParameters(properties)}}
{{(hasPasswordProfile ? EmitPasswordProfileParameters() : "")}}
{{HeaderParamDecls(naming)}}
{{GenericHeadersParamDecl()}}

{{AccessTokenParamDecl()}}

        protected override void ProcessRecord()
        {
            if (!ShouldProcess({{TargetId(naming)}}, "{{naming.VerbName}}"))
                return;

            var body = new {{entityType}}();
{{EmitPropertyAssignments(properties)}}
{{(hasPasswordProfile ? EmitPasswordProfileAssignment() : "")}}
{{AuthBlock}}

            {{entityType}}? result;
            try
            {
                result = {{EmitCallWithOptionalHeaders(naming, "PatchAsync", "body")}}.GetAwaiter().GetResult();
            }
{{CatchBlock(TargetId(naming))}}

            // Graph often answers a successful PATCH with 204 and no body (seen live on
            // schemaExtension update). Re-fetch so the cmdlet returns the updated resource
            // instead of nothing.
            if (result is null)
            {
                WriteVerbose("[MgPoC] PATCH succeeded with no response body, re-fetching the updated resource.");
                try
                {
                    result = client.{{naming.BuilderExpression}}.GetAsync().GetAwaiter().GetResult();
                }
{{CatchBlock(TargetId(naming), "    ")}}
            }

            WriteObject(result);
        }
    }
}

""";
    }

    public static string EmitRemove(CmdletNaming naming, EmitContext ctx)
    {
        ArgumentNullException.ThrowIfNull(naming);
        ArgumentNullException.ThrowIfNull(ctx);
        return $$"""
#nullable enable

using System;
using System.Management.Automation;
using System.Net.Http;
using Microsoft.Graph.PowerShell.Authentication.Helpers;
using {{ctx.ClientNamespace}};
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace {{ctx.CmdletNamespace}}
{
    [Cmdlet(VerbsCommon.{{naming.VerbName}}, "{{naming.Noun}}", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class {{naming.ClassName}} : PSCmdlet
    {
{{PathParams(naming)}}
{{HeaderParamDecls(naming)}}
{{GenericHeadersParamDecl()}}

{{AccessTokenParamDecl()}}

        protected override void ProcessRecord()
        {
            if (!ShouldProcess({{TargetId(naming)}}, "{{naming.VerbName}}"))
                return;
{{AuthBlock}}

            // DeleteAsync returns a plain Task: a standard delete response has no body.
            try
            {
                {{EmitCallWithOptionalHeaders(naming, "DeleteAsync", null)}}
                    .GetAwaiter().GetResult();
            }
{{CatchBlock(TargetId(naming))}}
        }
    }
}

""";
    }

    // Written once per module. Every cmdlet file relies on same-namespace visibility for these
    // helpers instead of carrying its own copy.
    public static string EmitSharedAuth(EmitContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return $$"""
#nullable enable

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace {{ctx.CmdletNamespace}}
{
    internal static class CmdletExtensions
    {
        public static bool IsParameterBound(this PSCmdlet cmdlet, string parameterName)
            => cmdlet.MyInvocation?.BoundParameters.ContainsKey(parameterName) ?? false;
    }

    // Minimal IAuthenticationProvider for the -AccessToken path: just stamps the bearer header
    // Kiota's own request-adapter pipeline expects, no token acquisition/refresh.
    internal sealed class StaticBearerTokenAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _token;

        public StaticBearerTokenAuthenticationProvider(string token)
        {
            _token = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token.Substring(7)
                : token;
        }

        public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            request.Headers.TryAdd("Authorization", $"Bearer {_token}");
            return Task.CompletedTask;
        }
    }
}
""";
    }

    private static string EmitPropertyParameters(IReadOnlyList<CmdletProperty> properties) =>
        string.Join("\n", properties.Select(p => $$"""

                [Parameter(Mandatory = false)]
                public {{p.PsTypeName}}? {{p.PascalName}} { get; set; }
        """));

    private static string EmitPropertyAssignments(IReadOnlyList<CmdletProperty> properties) =>
        string.Join("\n", properties.Select(p => $$"""

            if (this.IsParameterBound(nameof({{p.PascalName}})))
                body.{{p.PascalName}} = {{(p.IsArray ? $"{p.PascalName}!.ToList()" : p.PascalName)}};
        """));

    private static string EmitPasswordProfileParameters() => """

                [Parameter(Mandatory = false,
                    HelpMessage = "Required by Graph to create a user. Ignored if the resource has no passwordProfile.")]
                public string? Password { get; set; }

                [Parameter(Mandatory = false)]
                public bool? ForceChangePasswordNextSignIn { get; set; }
        """;

    private static string EmitPasswordProfileAssignment() => """

            if (this.IsParameterBound(nameof(Password)) || this.IsParameterBound(nameof(ForceChangePasswordNextSignIn)))
            {
                body.PasswordProfile = new PasswordProfile
                {
                    Password = Password,
                    ForceChangePasswordNextSignIn = ForceChangePasswordNextSignIn ?? true,
                };
            }
        """;
}
