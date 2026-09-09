using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Gloria.Application.Abstractions;

namespace Gloria.Infrastructure.Csv;

/// <summary>
/// Reads the source CSV files: ';' delimited, UTF-8 (BOM tolerated), invariant culture, headers on
/// the first line. Each data row is returned with its 1-based line number and the raw line text so
/// that bad rows can be quarantined verbatim.
/// </summary>
public sealed class SemicolonCsvFileReader : ICsvFileReader
{
    public IReadOnlyList<CsvRow> Read(Stream stream)
    {
        using var sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = sr.ReadToEnd();
        return Parse(text);
    }

    private static IReadOnlyList<CsvRow> Parse(string text)
    {
        var rawLines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
            DetectColumnCountChanges = false,
        };

        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, config);

        if (!csv.Read() || !csv.ReadHeader())
            return Array.Empty<CsvRow>();

        var headers = (csv.HeaderRecord ?? Array.Empty<string>())
            .Select(h => h.Trim().TrimStart('﻿'))
            .ToArray();

        var rows = new List<CsvRow>();
        while (csv.Read())
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
                fields[headers[i]] = csv.TryGetField<string>(i, out var value) ? (value ?? string.Empty).Trim() : string.Empty;

            // skip blank lines (e.g. a trailing newline at end of file)
            if (fields.Values.All(string.IsNullOrWhiteSpace))
                continue;

            var lineNumber = (int)csv.Parser.Row; // 1-based, includes header line
            var raw = lineNumber - 1 < rawLines.Length && lineNumber - 1 >= 0
                ? rawLines[lineNumber - 1]
                : string.Join(';', headers.Select(h => fields[h]));

            rows.Add(new CsvRow(lineNumber, raw, fields));
        }

        return rows;
    }
}
