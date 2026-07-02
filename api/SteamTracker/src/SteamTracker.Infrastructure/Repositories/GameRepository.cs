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
        var existing = await _context.Games.FindAsync(new object[] { game.AppId }, cancellationToken);
        if (existing is null)
        {
            _context.Games.Add(game);
        }
        else
        {
            // Detach the tracked copy and attach the passed entity as modified
            _context.Entry(existing).State = EntityState.Detached;
            _context.Entry(game).State = EntityState.Modified;

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
}
