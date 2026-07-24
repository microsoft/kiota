using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Kiota.Builder.PowerShellWrapper;

// Hand-tuned naming exceptions, kept as data with a cited source on every entry.
//
// The published Microsoft.Graph names are mostly algorithmic, but a few come from
// hand-written AutoRest directives in the msgraph-sdk-powershell module configs. Matching
// the published names 100% means mirroring those directives here.
//
// Keep this list short. Add an entry only when the published name cannot come out of the
// naming rules, and cite the directive that created it.
public static partial class NamingOverrides
{
    private enum OverrideKind
    {
        SuppressOperation,
        ReplaceNoun,
        StripNounPrefix,
    }

    private sealed record Entry(OverrideKind Kind, string? Method, string PathPrefix, bool ExactPath, string? Value, string Reason);

    private static readonly List<Entry> Entries =
    [
        // The SDK ships no Update cmdlet for /users/{id}/calendar. Its pipeline removes the
        // operation outright, in src/Calendar/Calendar.md: remove-path-by-operation
        // user_UpdateCalendar. The wrapper must not invent a cmdlet the SDK chose to drop.
        new(OverrideKind.SuppressOperation, "PATCH", "/users/{}/calendar", ExactPath: true, Value: null,
            Reason: "Calendar.md remove-path-by-operation user_UpdateCalendar"),

        // GET /users/{id}/calendar ships as Get-MgUserDefaultCalendar, renamed in
        // src/Calendar/Calendar.md: "^(User)(Calendar)$" -> "$1Default$2".
        new(OverrideKind.ReplaceNoun, "GET", "/users/{}/calendar", ExactPath: true, Value: "UserDefaultCalendar",
            Reason: "Calendar.md directive renames UserCalendar to UserDefaultCalendar"),

        // Every noun under /solutions/ drops the "Solution" prefix: Get-MgBookingBusiness,
        // not Get-MgSolutionBookingBusiness. From src/Bookings/Bookings.md:
        // "^Solution(.*)$" -> "$1", which applies to the whole module.
        new(OverrideKind.StripNounPrefix, Method: null, "/solutions/", ExactPath: false, Value: "Solution",
            Reason: "Bookings.md directive strips the Solution subject prefix module-wide"),
    ];

    [GeneratedRegex(@"\{[^}]*\}")]
    private static partial Regex PathParamRegex();

    // Parameter names are erased before comparing, so "/users/{user-id}/calendar" and
    // "/users/{id}/calendar" both match the "/users/{}/calendar" entries above. A spec-side
    // parameter rename must not silently disable an override.
    private static string NormalizePath(string pathTemplate) =>
        PathParamRegex().Replace(pathTemplate, "{}").TrimEnd('/').ToLowerInvariant();

    public static bool IsSuppressed(string httpMethod, string pathTemplate)
    {
        ArgumentNullException.ThrowIfNull(httpMethod);
        ArgumentNullException.ThrowIfNull(pathTemplate);
        var path = NormalizePath(pathTemplate);
        foreach (var entry in Entries)
        {
            if (entry.Kind == OverrideKind.SuppressOperation && Matches(entry, httpMethod, path))
                return true;
        }
        return false;
    }

    public static string ApplyNounOverrides(string httpMethod, string pathTemplate, string noun)
    {
        ArgumentNullException.ThrowIfNull(httpMethod);
        ArgumentNullException.ThrowIfNull(pathTemplate);
        ArgumentNullException.ThrowIfNull(noun);
        var path = NormalizePath(pathTemplate);
        foreach (var entry in Entries)
        {
            if (!Matches(entry, httpMethod, path))
                continue;
            switch (entry.Kind)
            {
                case OverrideKind.ReplaceNoun:
                    return entry.Value!;
                case OverrideKind.StripNounPrefix when noun.StartsWith(entry.Value!, StringComparison.Ordinal) && noun.Length > entry.Value!.Length:
                    noun = noun[entry.Value!.Length..];
                    break;
            }
        }
        return noun;
    }

    private static bool Matches(Entry entry, string httpMethod, string normalizedPath)
    {
        if (entry.Method is not null && !string.Equals(entry.Method, httpMethod, StringComparison.OrdinalIgnoreCase))
            return false;
        return entry.ExactPath
            ? string.Equals(normalizedPath, entry.PathPrefix, StringComparison.Ordinal)
            : normalizedPath.StartsWith(entry.PathPrefix, StringComparison.Ordinal);
    }
}
