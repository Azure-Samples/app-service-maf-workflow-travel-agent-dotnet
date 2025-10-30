namespace TravelPlanner.Shared.ExternalServices;

/// <summary>
/// Service for currency conversion
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Gets the exchange rate between two currencies
    /// </summary>
    Task<CurrencyConversion> GetExchangeRateAsync(
        string fromCurrency, 
        string toCurrency,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Converts an amount from one currency to another
    /// </summary>
    Task<CurrencyConversion> ConvertAmountAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a currency conversion
/// </summary>
public class CurrencyConversion
{
    public decimal OriginalAmount { get; set; }
    public string OriginalCurrency { get; set; } = string.Empty;
    public decimal ConvertedAmount { get; set; }
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime RateDate { get; set; }
    
    public string GetSummary()
    {
        return $"{OriginalAmount:N2} {OriginalCurrency} = {ConvertedAmount:N2} {TargetCurrency} " +
               $"(Rate: {ExchangeRate:F4} as of {RateDate:yyyy-MM-dd})";
    }
}
