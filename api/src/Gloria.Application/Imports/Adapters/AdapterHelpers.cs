using System.Globalization;
using System.Text.Json;
using Gloria.Application.Abstractions;

namespace Gloria.Application.Imports.Adapters;

internal static class AdapterHelpers
{
    public static string RawJson(CsvRow row) => JsonSerializer.Serialize(row.Fields);

    public static bool TryParseAmount(string? raw, out decimal amount) =>
        decimal.TryParse((raw ?? string.Empty).Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

    public static bool TryParseDate(string? raw, out DateTime date) =>
        DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date);

    public static bool TryParseDateTime(string? datePart, string? timePart, out DateTime dateTime)
    {
        var combined = string.IsNullOrWhiteSpace(timePart) ? datePart : $"{datePart} {timePart}";
        return DateTime.TryParse(combined, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateTime);
    }

    /// <summary>Turns a free-text description into a stable product-code token (A-Z0-9 and underscore).</summary>
    public static string Slug(string text)
    {
        var chars = text.Trim().ToUpperInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
            .ToArray();
        return new string(chars).Trim('_').Replace("__", "_");
    }
}
