namespace LeadPortal.Configuration;

/// <summary>
/// Controls seeding of sample leads on first run. Bound from the "Seeding" section.
/// </summary>
public class SeedingOptions
{
    public const string SectionName = "Seeding";

    /// <summary>When true, seed sample leads if the database is empty.</summary>
    public bool Enabled { get; set; } = true;
}
