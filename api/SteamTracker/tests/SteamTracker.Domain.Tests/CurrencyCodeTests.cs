using FluentAssertions;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Tests;

public class CurrencyCodeTests
{
    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("rub")]
    [InlineData("brl")]
    public void Constructor_accepts_valid_codes(string code)
    {
        var currency = new CurrencyCode(code);
        currency.Value.Should().Be(code.ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Constructor_rejects_empty_codes(string code)
    {
        var act = () => new CurrencyCode(code);
        act.Should().Throw<ArgumentException>().WithMessage("Currency code cannot be empty.*");
    }

    [Theory]
    [InlineData("XXX")]
    [InlineData("BTC")]
    public void Constructor_rejects_invalid_codes(string code)
    {
        var act = () => new CurrencyCode(code);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Unsupported currency code:*");
    }

    [Fact]
    public void Implicit_conversion_to_string()
    {
        CurrencyCode code = new CurrencyCode("USD");
        string s = code;
        s.Should().Be("USD");
    }
}
