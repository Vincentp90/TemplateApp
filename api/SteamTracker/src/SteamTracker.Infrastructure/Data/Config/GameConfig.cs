using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Infrastructure.Data.Config;

public class GameConfig : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.AppId);

        builder.Property(g => g.Name).HasMaxLength(256).IsRequired();
        
        // Store CurrentPrice as separate columns with shadow properties
        builder.Property<decimal?>("CurrentPriceAmount")
            .HasColumnName("current_price_amount")
            .HasPrecision(10, 2)
            .IsRequired(false);
        builder.Property<string>("CurrentPriceCurrency")
            .HasColumnName("current_price_currency")
            .HasMaxLength(3)
            .IsRequired(false);

        // Convert Money value object to/from "Amount|Currency" string
        builder.Property<Money?>("CurrentPrice")
            .HasConversion(
                v => v == null ? null : $"{v.Value.Amount}|{v.Value.Currency}",
                v => v == null ? null : ParseMoney(v!));

        builder.HasMany(g => g.PriceSnapshots)
            .WithOne()
            .HasForeignKey(ps => ps.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static Money? ParseMoney(string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 2) return null;
        return new Money(decimal.Parse(parts[0]), parts[1]);
    }
}
