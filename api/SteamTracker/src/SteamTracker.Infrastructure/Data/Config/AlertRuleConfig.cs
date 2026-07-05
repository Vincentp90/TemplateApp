using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Infrastructure.Data.Config;

public class AlertRuleConfig : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.HasKey(ar => ar.AlertRuleId);

        builder.Property(ar => ar.UserId).HasMaxLength(128).IsRequired();

        // Convert SteamAppId value object to/from int
        builder.Property(ar => ar.AppId)
            .HasConversion(
                v => v.Value,
                v => new SteamAppId(v));

        // Convert Money value object to/from "Amount|Currency" string
        builder.Property(ar => ar.TriggerBelowPrice)
            .HasConversion(
                v => $"{v.Amount}|{v.Currency.Value}",
                v => ParseMoney(v));

        builder.Property(ar => ar.LastTriggeredAt).IsRequired(false);
    }

    private static Money ParseMoney(string value)
    {
        var parts = value.Split('|');
        return new Money(decimal.Parse(parts[0]), parts[1]);
    }
}
