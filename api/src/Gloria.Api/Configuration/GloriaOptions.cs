namespace Gloria.Api.Configuration;

public sealed class GloriaOptions
{
    public const string SectionName = "Gloria";

    /// <summary>Apply pending EF Core migrations on startup (this also seeds reference data via HasData).</summary>
    public bool AutoMigrate { get; set; } = true;
}
