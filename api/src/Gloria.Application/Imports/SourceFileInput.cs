using Gloria.Application.Abstractions;

namespace Gloria.Application.Imports;

/// <summary>One uploaded source file for an end-of-day run. The orchestrator only calls <see cref="ReadRows"/>.</summary>
public sealed record SourceFileInput(string SourceCode, string DisplayName, Stream Content)
{
    public IReadOnlyList<CsvRow> ReadRows(ICsvFileReader reader) => reader.Read(Content);
}
