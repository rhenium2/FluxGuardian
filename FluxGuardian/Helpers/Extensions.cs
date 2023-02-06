using System.Text.RegularExpressions;

namespace FluxGuardian.Helpers;

public static class Extensions
{
    public static string ToRelativeText(this DateTime dateTime)
    {
        return (DateTime.UtcNow - dateTime).ToReadableString() + " ago";
    }

    public static string ToReadableString(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalMilliseconds == 0)
        {
            return string.Empty;
        }

        (string unit, int value) = new Dictionary<string, int>
        {
            { "year(s)", (int)(timeSpan.TotalDays / 365.25) }, //https://en.wikipedia.org/wiki/Year#Intercalation
            { "month(s)", (int)(timeSpan.TotalDays / 29.53) }, //https://en.wikipedia.org/wiki/Month
            { "day(s)", (int)timeSpan.TotalDays },
            { "hour(s)", (int)timeSpan.TotalHours },
            { "minute(s)", (int)timeSpan.TotalMinutes },
            { "second(s)", (int)timeSpan.TotalSeconds },
            { "millisecond(s)", (int)timeSpan.TotalMilliseconds }
        }.First(kvp => kvp.Value > 0);

        return $"{value} {unit}";
    }

    public static bool IsValidFluxPortString(string portText)
    {
        var fluxApiPorts = new[] { 16127, 16137, 16147, 16157, 16167, 16177, 16187, 16197 };
        return int.TryParse(portText, out var port) && fluxApiPorts.Contains(port);
    }

    public static bool IsValidIPString(string ip)
    {
        var validateIPv4Regex =
            new Regex(
                "^(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
        return validateIPv4Regex.IsMatch(ip);
    }
}