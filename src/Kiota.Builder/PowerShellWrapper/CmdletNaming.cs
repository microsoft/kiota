using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PowerShellWrapper;

public sealed record HeaderParam(string RawName, string PsName);

public sealed record CmdletNaming(
    string VerbsClass,
    string VerbName,
    string Noun,
    string ClassName,
    IReadOnlyList<string> PathParamNames,
    string BuilderExpression,
    IReadOnlyList<HeaderParam> HeaderParams);

public static class Naming
{
    // GET->Get, POST->New, PATCH->Update, PUT->Set, DELETE->Remove (design spec section 7).
    // The attribute class is tracked too because it differs per verb: Update lives in
    // VerbsData, the rest in VerbsCommon.
    private static readonly Dictionary<string, (string VerbsClass, string VerbName)> VerbMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["get"] = ("VerbsCommon", "Get"),
        ["post"] = ("VerbsCommon", "New"),
        ["patch"] = ("VerbsData", "Update"),
        ["put"] = ("VerbsCommon", "Set"),
        ["delete"] = ("VerbsCommon", "Remove"),
    };

    public static CmdletNaming Resolve(OperationInfo operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (!VerbMap.TryGetValue(operation.HttpMethod, out var verb))
            throw new NotSupportedException($"No cmdlet verb mapping for HTTP method '{operation.HttpMethod}'.");

        // The noun comes from the URL path, not the operationId. OperationIds keep whatever
        // plurality the spec author chose, while the published SDK names follow the path:
        // GET /users/{id}/messages is Get-MgUserMessage. The few hand-tuned exceptions the
        // published SDK carries are mirrored as data in NamingOverrides, never as code here.
        var noun = "Mg" + NamingOverrides.ApplyNounOverrides(operation.HttpMethod, operation.Path, BuildNounFromPath(operation.Path));

        // A list GET (/users/{id}/messages) and its item GET (/users/{id}/messages/{message-id})
        // get the same noun on purpose. PowerShellWrapperGenerationService pairs them into one
        // public Get-MgX dispatcher cmdlet; the two real implementations get suffixed names via
        // WithSuffix below.
        var className = $"{verb.VerbName}{noun}Command";

        var pathParamNames = ExtractPathParamNames(operation.Path);
        var builderExpression = BuildBuilderExpression(operation.Path, pathParamNames);
        var headerParams = (operation.HeaderParams ?? [])
            .Select(raw => new HeaderParam(raw, raw.ToPascalCase('-')))
            .ToList();

        return new CmdletNaming(verb.VerbsClass, verb.VerbName, noun, className, pathParamNames, builderExpression, headerParams);
    }

    // Names one of the two internal cmdlets behind a paired GET dispatcher, e.g.
    // Get-MgUserMessage_List. The public dispatcher keeps the bare noun.
    public static CmdletNaming WithSuffix(CmdletNaming naming, string suffix)
    {
        ArgumentNullException.ThrowIfNull(naming);
        return naming with
        {
            Noun = naming.Noun + suffix,
            ClassName = $"{naming.VerbName}{naming.Noun}{suffix}Command",
        };
    }

    // Pascal-cases and singularizes every fixed path segment, then joins them. Two kinds of
    // repetition are dropped, because the published SDK drops them:
    //   /sites/{id}/sites                  -> Site (not SiteSite)
    //   /domains/{id}/domainNameReferences -> DomainNameReference (the shared word "Domain"
    //                                         appears once, matching Get-MgDomainNameReference)
    // An OData cast segment like graph.user becomes AsUser, matching Get-MgGroupOwnerAsUser.
    // Cast type names are already singular, so they skip the singularizer.
    private static string BuildNounFromPath(string path)
    {
        var parts = new List<string>();
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.StartsWith('{') && segment.EndsWith('}'))
                continue;

            var castType = segment.StartsWith("microsoft.graph.", StringComparison.OrdinalIgnoreCase)
                ? segment["microsoft.graph.".Length..]
                : segment.StartsWith("graph.", StringComparison.OrdinalIgnoreCase)
                    ? segment["graph.".Length..]
                    : null;
            if (castType is not null)
            {
                parts.Add("As" + castType.ToFirstCharacterUpperCase());
                continue;
            }

            var part = Singularizer.SingularizeSegment(segment.ToFirstCharacterUpperCase());
            if (parts.Count > 0)
            {
                var previous = parts[^1];
                if (string.Equals(previous, part, StringComparison.Ordinal))
                    continue;
                var boundaryWord = Singularizer.TrailingWord(previous);
                if (string.Equals(part, boundaryWord, StringComparison.Ordinal))
                    continue;
                if (part.StartsWith(boundaryWord, StringComparison.Ordinal) && part.Length > boundaryWord.Length)
                    part = part[boundaryWord.Length..];
            }
            parts.Add(part);
        }
        return string.Concat(parts);
    }

    private static List<string> ExtractPathParamNames(string path)
    {
        var names = new List<string>();
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.StartsWith('{') && segment.EndsWith('}'))
                names.Add(segment[1..^1].ToPascalCase('-'));
        }
        return names;
    }

    // The Kiota-generated ApiClient exposes a property per fixed path segment and an indexer
    // per path parameter, e.g. client.Users[UserId].Messages[MessageId].
    private static string BuildBuilderExpression(string path, List<string> pathParamNames)
    {
        var expression = new System.Text.StringBuilder();
        var paramIndex = 0;
        var first = true;

        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.StartsWith('{') && segment.EndsWith('}'))
            {
                expression.Append('[').Append(pathParamNames[paramIndex++]).Append(']');
            }
            else
            {
                if (!first)
                    expression.Append('.');
                expression.Append(segment.ToFirstCharacterUpperCase());
            }
            first = false;
        }

        return expression.ToString();
    }
}
