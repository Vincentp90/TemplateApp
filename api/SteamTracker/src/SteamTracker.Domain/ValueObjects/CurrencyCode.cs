namespace SteamTracker.Domain.ValueObjects;

/// <summary>
/// Represents a closed set of valid currency codes.
/// </summary>
public readonly record struct CurrencyCode
{
    private static readonly HashSet<string> ValidCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "EUR", "USD", "GBP", "RUB", "BRL", "UAH", "KZT", "TRY", "JPY", "CNY",
        "AUD", "CAD", "CHF", "SEK", "NOK", "DKK", "PLN", "CZK", "HUF", "RON",
        "INR", "KRW", "MXN", "SGD", "NZD", "ZAR", "THB", "PHP", "IDR", "MYR"
    };

    public string Value { get; }

    public CurrencyCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code cannot be empty.", nameof(code));

        var normalized = code.ToUpperInvariant();
        if (!ValidCodes.Contains(normalized))
            throw new ArgumentException($"Unsupported currency code: {code}", nameof(code));

        Value = normalized;
    }

    public static implicit operator string(CurrencyCode code) => code.Value;
}
