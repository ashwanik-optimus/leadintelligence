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

    /// <summary>Maximum accepted length for name/company text fields.</summary>
    public int MaxFieldLength { get; set; } = 200;

    /// <summary>Maximum accepted length for the email address (RFC 5321 caps at 320).</summary>
    public int MaxEmailLength { get; set; } = 320;
}
