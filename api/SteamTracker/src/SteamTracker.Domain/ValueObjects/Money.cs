namespace SteamTracker.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with an amount and currency.
/// </summary>
public readonly struct Money : IEquatable<Money>
{
    public static Money Free { get; } = new(0m, new CurrencyCode("EUR")) { IsFree = true };

    public decimal Amount { get; }
    public CurrencyCode Currency { get; }
    public bool IsFree { get; init; }

    public Money(decimal amount, CurrencyCode currency)
    {
        if (amount < 0m)
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Convenience constructor with default EUR — delegates currency validation to <see cref="CurrencyCode"/>.
    /// </summary>
    public Money(decimal amount, string currency = "EUR")
        : this(amount, new CurrencyCode(currency)) { }

    public bool Equals(Money other)
    {
        if (IsFree != other.IsFree) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public static bool operator ==(Money a, Money b) => a.Equals(b);
    public static bool operator !=(Money a, Money b) => !a.Equals(b);

    public static bool operator <(Money a, Money b)
    {
        if (a.IsFree) return true;
        if (b.IsFree) return false;
        return a.Amount < b.Amount;
    }

    public static bool operator >(Money a, Money b)
    {
        if (a.IsFree) return false;
        if (b.IsFree) return true;
        return a.Amount > b.Amount;
    }

    public static bool operator <=(Money a, Money b)
    {
        if (a.IsFree) return true;
        if (b.IsFree) return false;
        return a.Amount <= b.Amount;
    }

    public static bool operator >=(Money a, Money b) => !(a < b);
}
