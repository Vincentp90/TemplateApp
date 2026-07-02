namespace SteamTracker.Domain.ValueObjects;

/// <summary>
/// Wraps a Guid as a user identifier.
/// </summary>
public readonly record struct UserId
{
    public Guid Value { get; }

    public UserId(Guid value) => Value = value;
    public UserId(string value) => Value = Guid.Parse(value);

    public static implicit operator UserId(Guid value) => new(value);
    public static explicit operator Guid(UserId id) => id.Value;
}
