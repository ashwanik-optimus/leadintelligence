using LeadPortal.Models;

namespace LeadPortal.Configuration;

/// <summary>
/// Maps each budget band to its estimated pipeline value. Bound from the
/// "BudgetValuation" section so the valuation model can change without code edits.
/// </summary>
public class BudgetValuationOptions
{
    public const string SectionName = "BudgetValuation";

    public decimal Under10k { get; set; } = 5_000m;
    public decimal From10kTo50k { get; set; } = 30_000m;
    public decimal Over50k { get; set; } = 75_000m;

    /// <summary>Returns the configured estimated value for a budget band.</summary>
    public decimal ValueFor(BudgetBand band) => band switch
    {
        BudgetBand.Under10k     => Under10k,
        BudgetBand.From10kTo50k => From10kTo50k,
        BudgetBand.Over50k      => Over50k,
        _                        => 0m
    };
}
