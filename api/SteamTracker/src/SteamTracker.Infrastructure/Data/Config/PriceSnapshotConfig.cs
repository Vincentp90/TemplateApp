using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Infrastructure.Data.Config;

public class PriceSnapshotConfig : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> builder)
    {
        builder.HasKey(ps => ps.SnapshotId);

        builder.Property(ps => ps.GameId).IsRequired();

        // Convert Money value object to/from "Amount|Currency" string
        builder.Property(ps => ps.Price)
            .HasConversion(
                v => $"{v.Amount}|{v.Currency.Value}",
                v => ParseMoney(v));
    }

    private static Money ParseMoney(string value)
    {
        var parts = value.Split('|');
        return new Money(decimal.Parse(parts[0]), parts[1]);
    }
}
