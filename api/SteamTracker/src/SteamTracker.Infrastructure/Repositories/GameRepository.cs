using Microsoft.EntityFrameworkCore;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;

namespace SteamTracker.Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly SteamTrackerDbContext _context;

    public GameRepository(SteamTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetAsync(SteamAppId appId, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .AsNoTracking()
            .Include(g => g.PriceSnapshots)
            .FirstOrDefaultAsync(g => g.AppId == appId, cancellationToken);
    }

    public async Task SaveAsync(Game game, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Games.FirstOrDefaultAsync(g => g.AppId == game.AppId, cancellationToken);
        if (existing is null)
        {
            _context.Games.Add(game);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(game);

            // Persist only PriceSnapshots that don't already exist in the DB
            foreach (var snapshot in game.PriceSnapshots)
            {
                var existingSnapshot = await _context.PriceSnapshots
                    .AnyAsync(s => s.SnapshotId == snapshot.SnapshotId, cancellationToken);
                if (!existingSnapshot)
                {
                    _context.PriceSnapshots.Add(snapshot);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SteamAppId>> GetAppIdsDueForPriceCheckAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var cutoff = now - TimeSpan.FromHours(24);

        return await _context.Games
            .AsNoTracking()
            .Where(g => g.LastCheckedAt == null || g.LastCheckedAt < cutoff)
            .Select(g => g.AppId)
            .ToListAsync(cancellationToken);
    }
}
