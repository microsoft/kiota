using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Kiota.Builder.PowerShellWrapper;

// Singularizes path segments so cmdlet nouns match the published Microsoft.Graph names.
//
// This is not a general English inflector. The vocabulary is the finite set of Graph path
// segments, and every rule exists because a shipping cmdlet needs it. The README in this
// folder lists each rule with the cmdlet that proves it.
//
// One behavior matters most: the published SDK singularizes every camel-case word in a
// segment, not just the last one. "termsAndConditions" ships as TermAndCondition and
// "onPremisesSynchronization" as OnPremiseSynchronization. SingularizeSegment does the same:
// it splits a segment into words and runs the rules on each word.
public static partial class Singularizer
{
    // Irregular plurals the SDK singularizes: Get-MgDriveItemChild, Get-MgUserPerson.
    private static readonly Dictionary<string, string> Irregulars = new(StringComparer.Ordinal)
    {
        ["Children"] = "Child",
        ["People"] = "Person",
    };

    // Words that end in "s" but are not plurals. The SDK keeps them as-is:
    // /users/{id}/settings/windows ships as Get-MgUserSettingWindows.
    private static readonly HashSet<string> Invariants = new(StringComparer.Ordinal)
    {
        "Windows",
    };

    // Splits Pascal or camel text into words. Handles acronym runs ("OS" in "MacOSDmgApp"),
    // trailing digits ("Win32"), and a leading lowercase word.
    [GeneratedRegex("[A-Z]+(?![a-z])|[A-Z][a-z0-9]*|[a-z0-9]+")]
    private static partial Regex WordRegex();

    // Segments with a version tag: "alerts_v2" must become "AlertV2" (Get-MgSecurityAlertV2).
    // Underscores are not legal in a cmdlet noun anyway.
    [GeneratedRegex(@"^(?<stem>.+)_[vV](?<version>\d+)$")]
    private static partial Regex VersionTagRegex();

    // Singularizes one already-Pascal-cased path segment, word by word.
    public static string SingularizeSegment(string pascalSegment)
    {
        ArgumentNullException.ThrowIfNull(pascalSegment);

        var versionTag = VersionTagRegex().Match(pascalSegment);
        if (versionTag.Success)
            return SingularizeSegment(versionTag.Groups["stem"].Value) + "V" + versionTag.Groups["version"].Value;

        var result = new StringBuilder(pascalSegment.Length);
        foreach (Match word in WordRegex().Matches(pascalSegment))
            result.Append(SingularizeWord(word.Value));
        return result.ToString();
    }

    // Returns the last camel-case word of a noun part: "Workflow" for "LifecycleWorkflow".
    // BuildNounFromPath uses this to spot a word repeated across a segment boundary.
    public static string TrailingWord(string pascalText)
    {
        ArgumentNullException.ThrowIfNull(pascalText);
        var matches = WordRegex().Matches(pascalText);
        return matches.Count > 0 ? matches[^1].Value : pascalText;
    }

    // Ordered rules; first match wins, so the order is part of the algorithm. The README's
    // rule table gives the shipping cmdlet behind each rule.
    public static string SingularizeWord(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        if (word.Length < 3)
            return word;
        if (IsAllUpper(word))
            return word; // acronyms ("OS", "SMS") are never plural forms
        if (Irregulars.TryGetValue(word, out var irregular))
            return irregular;
        if (Invariants.Contains(word))
            return word;
        if (word.EndsWith("ies", StringComparison.Ordinal) && word.Length > 3)
            return word[..^3] + "y";                                   // Policies -> Policy
        if (word.EndsWith("uses", StringComparison.Ordinal) && word.Length > 4)
            return word[..^2];                                         // Statuses -> Status
        if (EndsWithSibilantEs(word))
            return word[..^2];                                         // Businesses -> Business, Mailboxes -> Mailbox
        if (word.EndsWith("ss", StringComparison.Ordinal) || word.EndsWith("us", StringComparison.Ordinal) || word.EndsWith("is", StringComparison.Ordinal))
            return word;                                               // Access, Status, Analysis stay put
        if (word.EndsWith('s'))
            return word[..^1];                                         // Messages -> Message, Plans -> Plan
        return word;
    }

    private static bool EndsWithSibilantEs(string word)
    {
        if (!word.EndsWith("es", StringComparison.Ordinal) || word.Length < 4)
            return false;
        var stem = word[..^2];
        return stem.EndsWith('x')
            || stem.EndsWith('z')
            || stem.EndsWith("ch", StringComparison.Ordinal)
            || stem.EndsWith("sh", StringComparison.Ordinal)
            || stem.EndsWith("ss", StringComparison.Ordinal);
    }

    private static bool IsAllUpper(string word)
    {
        foreach (var c in word)
        {
            if (char.IsLower(c))
                return false;
        }
        return true;
    }
}
