using FluentAssertions;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Tests;

public class UserIdTests
{
    [Fact]
    public void Constructor_wraps_guid()
    {
        var guid = Guid.NewGuid();
        var userId = new UserId(guid);
        userId.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_wraps_guid_string()
    {
        var guidStr = Guid.NewGuid().ToString();
        var userId = new UserId(guidStr);
        userId.Value.Should().Be(Guid.Parse(guidStr));
    }

    [Fact]
    public void Equality_succeeds_when_same_guid()
    {
        var guid = Guid.NewGuid();
        var a = new UserId(guid);
        var b = new UserId(guid);
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_fails_when_different_guid()
    {
        var a = new UserId(Guid.NewGuid());
        var b = new UserId(Guid.NewGuid());
        a.Should().NotBe(b);
    }

    [Fact]
    public void Implicit_conversion_from_guid()
    {
        var guid = Guid.NewGuid();
        UserId userId = guid;
        userId.Value.Should().Be(guid);
    }

    [Fact]
    public void Explicit_conversion_to_guid()
    {
        var guid = Guid.NewGuid();
        UserId userId = new UserId(guid);
        Guid result = (Guid)userId;
        result.Should().Be(guid);
    }
}
