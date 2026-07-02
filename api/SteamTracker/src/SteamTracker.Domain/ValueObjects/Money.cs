namespace SteamTracker.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with an amount and currency.
/// </summary>
public readonly struct Money : IEquatable<Money>
{
    public static Money Free { get; } = new(0m, "EUR") { IsFree = true };

    public decimal Amount { get; }
    public string Currency { get; }
    public bool IsFree { get; init; }

    public Money(decimal amount, string currency = "EUR")
    {
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

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
