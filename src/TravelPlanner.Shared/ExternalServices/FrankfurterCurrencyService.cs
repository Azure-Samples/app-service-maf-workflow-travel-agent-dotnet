using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace TravelPlanner.Shared.ExternalServices;

/// <summary>
/// Frankfurter API implementation - Free currency conversion using ECB data, no API key required
/// </summary>
public class FrankfurterCurrencyService : ICurrencyService
{
    private const string API_BASE = "https://api.frankfurter.app";
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterCurrencyService> _logger;
    
    public FrankfurterCurrencyService(HttpClient httpClient, ILogger<FrankfurterCurrencyService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<CurrencyConversion> GetExchangeRateAsync(
        string fromCurrency, 
        string toCurrency,
        CancellationToken cancellationToken = default)
    {
        return await ConvertAmountAsync(1.0m, fromCurrency, toCurrency, cancellationToken);
    }
    
    public async Task<CurrencyConversion> ConvertAmountAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Converting {Amount} {From} to {To}", amount, fromCurrency, toCurrency);
            
            var url = $"{API_BASE}/latest?amount={amount}&from={fromCurrency.ToUpper()}&to={toCurrency.ToUpper()}";
            var response = await _httpClient.GetFromJsonAsync<FrankfurterResponse>(url, cancellationToken);
            
            if (response?.Rates == null || !response.Rates.ContainsKey(toCurrency.ToUpper()))
            {
                _logger.LogWarning("No exchange rate found for {From} to {To}", fromCurrency, toCurrency);
                
                // Fallback: return 1:1 if conversion fails
                return new CurrencyConversion
                {
                    OriginalAmount = amount,
                    OriginalCurrency = fromCurrency.ToUpper(),
                    ConvertedAmount = amount,
                    TargetCurrency = toCurrency.ToUpper(),
                    ExchangeRate = 1.0m,
                    RateDate = DateTime.UtcNow
                };
            }
            
            var convertedAmount = response.Rates[toCurrency.ToUpper()];
            var exchangeRate = amount > 0 ? convertedAmount / amount : 0;
            
            _logger.LogInformation("Conversion successful: {Amount} {From} = {Converted} {To}", 
                amount, fromCurrency, convertedAmount, toCurrency);
            
            return new CurrencyConversion
            {
                OriginalAmount = amount,
                OriginalCurrency = fromCurrency.ToUpper(),
                ConvertedAmount = convertedAmount,
                TargetCurrency = toCurrency.ToUpper(),
                ExchangeRate = exchangeRate,
                RateDate = response.Date
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during currency conversion");
            
            // Fallback
            return new CurrencyConversion
            {
                OriginalAmount = amount,
                OriginalCurrency = fromCurrency.ToUpper(),
                ConvertedAmount = amount,
                TargetCurrency = toCurrency.ToUpper(),
                ExchangeRate = 1.0m,
                RateDate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during currency conversion");
            throw;
        }
    }
    
    // Frankfurter API response model
    private class FrankfurterResponse
    {
        public decimal Amount { get; set; }
        public string Base { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
