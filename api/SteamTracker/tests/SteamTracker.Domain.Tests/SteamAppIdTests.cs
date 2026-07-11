using FluentAssertions;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Tests;

public class SteamAppIdTests
{
    [Fact]
    public void Constructor_throws_when_zero()
    {
        var act = () => new SteamAppId(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_throws_when_negative()
    {
        var act = () => new SteamAppId(-1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_succeeds_when_positive()
    {
        var id = new SteamAppId(123456);
        id.Value.Should().Be(123456);
    }

    [Fact]
    public void Equality_succeeds_when_same_value()
    {
        var a = new SteamAppId(42);
        var b = new SteamAppId(42);
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_fails_when_different_value()
    {
        var a = new SteamAppId(42);
        var b = new SteamAppId(43);
        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_with_null_returns_false()
    {
        var a = new SteamAppId(42);
        object? b = null;
        a.Should().NotBe(b);
    }

    [Fact]
    public void GetHashCode_returns_consistent_value()
    {
        var a = new SteamAppId(42);
        var b = new SteamAppId(42);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Implicit_conversion_from_int()
    {
        SteamAppId id = 789;
        id.Value.Should().Be(789);
    }

    [Fact]
    public void Explicit_conversion_to_int()
    {
        SteamAppId id = new SteamAppId(789);
        int value = (int)id;
        value.Should().Be(789);
    }
}
