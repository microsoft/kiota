using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Kiota.Builder.Writers.Dart;

internal static partial class DartDefaultValueUtils
{
    private const long MicrosecondsPerDay = 86_400_000_000;
    private const long MaximumMicroseconds = 100_000_000 * MicrosecondsPerDay;

    // Dart accepts compact dates, extended years, fractional seconds, and overflowing
    // components. A .NET date parser would reject some defaults that Dart can parse.
    [GeneratedRegex(@"\A(?<year>[+-]?[0-9]{4,6})-?(?<month>[0-9]{2})-?(?<day>[0-9]{2})(?:[ T](?<hour>[0-9]{2})(?::?(?<minute>[0-9]{2})(?::?(?<second>[0-9]{2})(?:[.,](?<fraction>[0-9]+))?)?)?(?: ?(?<zone>[zZ]|(?<sign>[+-])(?<zoneHour>[0-9]{2})(?::?(?<zoneMinute>[0-9]{2}))?))?)?\z", RegexOptions.NonBacktracking, 500)]
    private static partial Regex DateTimePattern();

    internal static bool CanParseDateTime(string value)
    {
        var match = DateTimePattern().Match(value);
        if (!match.Success)
            return false;
        var year = ParseComponent(match, "year");
        // Even component overflow cannot bring more distant years into Dart's range.
        if (year is < -271822 or > 275761)
            return false;

        // Gregorian calendars repeat every 400 years. Map into .NET's supported
        // range, then account for the cycles without limiting Dart's extended years.
        var anchorYear = 2000 + (year % 400 + 400) % 400;
        var date = new DateTime(anchorYear, 1, 1)
            .AddMonths(ParseComponent(match, "month") - 1)
            .AddDays(ParseComponent(match, "day") - 1);
        var microseconds = (date.Ticks - DateTime.UnixEpoch.Ticks) / 10 +
            (year - (long)anchorYear) / 400 * 146097 * MicrosecondsPerDay;
        var minutes = ParseComponent(match, "hour") * 60 + ParseComponent(match, "minute");
        if (match.Groups["sign"].Success)
        {
            var offset = ParseComponent(match, "zoneHour") * 60 + ParseComponent(match, "zoneMinute");
            minutes -= match.Groups["sign"].Value == "-" ? -offset : offset;
        }
        microseconds += (minutes * 60L + ParseComponent(match, "second")) * 1_000_000;
        var fraction = match.Groups["fraction"].ValueSpan;
        var fractionalMicroseconds = 0;
        for (var i = 0; i < 6; i++)
            fractionalMicroseconds = fractionalMicroseconds * 10 + (i < fraction.Length ? fraction[i] - '0' : 0);
        microseconds += fractionalMicroseconds;
        // The generated client's local timezone is unknown. Keep local values away
        // from the range boundaries so timezone conversion cannot make them throw.
        var limit = match.Groups["zone"].Success ? MaximumMicroseconds : MaximumMicroseconds - MicrosecondsPerDay;
        return microseconds >= -limit && microseconds <= limit;
    }

    private static int ParseComponent(Match match, string name) =>
        match.Groups[name].Success ? int.Parse(match.Groups[name].ValueSpan, CultureInfo.InvariantCulture) : 0;
}
