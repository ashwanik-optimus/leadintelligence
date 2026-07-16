namespace LeadPortal.Configuration;

/// <summary>
/// Cross-origin policy. Bound from the "Cors" section. When no origins are configured,
/// no cross-origin access is granted (the SPA is served same-origin, so it is unaffected).
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
