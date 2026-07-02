using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SteamTracker.Domain.Entities;

namespace SteamTracker.Infrastructure.Data.Config;

public class TrackedGameConfig : IEntityTypeConfiguration<TrackedGame>
{
    public void Configure(EntityTypeBuilder<TrackedGame> builder)
    {
        builder.HasKey(tg => tg.AppId);

        builder.Property(tg => tg.TrackedSince).IsRequired();
    }
}
