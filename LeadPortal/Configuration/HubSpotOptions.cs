using System.ComponentModel.DataAnnotations;

namespace LeadPortal.Configuration;

/// <summary>
/// Configuration for the HubSpot integration (mock and, later, the real client).
/// Bound from the "HubSpot" configuration section.
/// </summary>
public class HubSpotOptions
{
    public const string SectionName = "HubSpot";

    /// <summary>Integration mode, surfaced to the dashboard router control (e.g. "Mock", "Live").</summary>
    [Required]
    public string Mode { get; set; } = "Mock";

    /// <summary>Human-readable connection status shown on the dashboard.</summary>
    [Required]
    public string StatusLabel { get; set; } = "Connected — Mock Mode";

    /// <summary>Lower bound (inclusive) of simulated sync latency, in milliseconds.</summary>
    [Range(0, 60_000)]
    public int MinLatencyMs { get; set; } = 800;

    /// <summary>Upper bound (inclusive) of simulated sync latency, in milliseconds.</summary>
    [Range(0, 60_000)]
    public int MaxLatencyMs { get; set; } = 1_500;

    /// <summary>Probability (0–1) that a mock sync fails, to exercise the failure path.</summary>
    [Range(0.0, 1.0)]
    public double FailureRate { get; set; } = 0.15;

    /// <summary>Prefix used to build fake contact ids returned by the mock client.</summary>
    [Required]
    public string MockContactIdPrefix { get; set; } = "hs-mock-";

    /// <summary>Total attempts (initial + retries) the background worker makes per lead.</summary>
    [Range(1, 10)]
    public int MaxSyncAttempts { get; set; } = 3;

    /// <summary>Base delay between retry attempts, in milliseconds (multiplied by attempt number).</summary>
    [Range(0, 60_000)]
    public int RetryDelayMs { get; set; } = 500;

    // Reserved for the real client (OAuth + CRM API). Empty while running in mock mode.
    public string? BaseUrl { get; set; }
    public string? AccessToken { get; set; }
}
