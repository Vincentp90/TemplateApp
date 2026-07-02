namespace SteamTracker.Domain.ValueObjects;

/// <summary>
/// Represents a Steam application ID — a positive integer.
/// </summary>
public readonly record struct SteamAppId
{
    public int Value { get; }

    public SteamAppId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("SteamAppId must be greater than zero.", nameof(value));

        Value = value;
    }

    public static implicit operator SteamAppId(int value) => new(value);
    public static explicit operator int(SteamAppId id) => id.Value;
}
