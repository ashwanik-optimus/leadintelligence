namespace LeadPortal.Configuration;

/// <summary>
/// Rules applied when validating an incoming lead. Bound from the "LeadValidation" section.
/// </summary>
public class LeadValidationOptions
{
    public const string SectionName = "LeadValidation";

    /// <summary>
    /// Free/consumer email domains that are rejected so only corporate addresses are accepted.
    /// </summary>
    public string[] BlockedEmailDomains { get; set; } = [];
}
