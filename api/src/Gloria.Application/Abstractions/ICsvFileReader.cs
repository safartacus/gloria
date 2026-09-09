namespace Gloria.Application.Abstractions;

/// <summary>One parsed CSV data row: its 1-based line number, the raw line, and header→value map.</summary>
public sealed record CsvRow(int LineNumber, string Raw, IReadOnlyDictionary<string, string> Fields)
{
    public string? Get(string header) => Fields.TryGetValue(header, out var v) ? v : null;

    public string Required(string header)
    {
        var v = Get(header);
        if (string.IsNullOrWhiteSpace(v))
            throw new CsvFieldMissingException(header);
        return v.Trim();
    }
}

public sealed class CsvFieldMissingException : Exception
{
    public string Header { get; }
    public CsvFieldMissingException(string header) : base($"Required column '{header}' is missing or empty.")
        => Header = header;
}

/// <summary>Reads a delimited text stream (an uploaded file) into <see cref="CsvRow"/> records.</summary>
public interface ICsvFileReader
{
    /// <summary>Reads from an already-open stream. The stream is not disposed.</summary>
    IReadOnlyList<CsvRow> Read(Stream stream);
}
