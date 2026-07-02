using Microsoft.EntityFrameworkCore;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;

namespace SteamTracker.Infrastructure.Repositories;

public class TrackedGameRepository : ITrackedGameRepository
{
    private readonly SteamTrackerDbContext _context;

    public TrackedGameRepository(SteamTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<TrackedGame?> GetAsync(SteamAppId appId, CancellationToken cancellationToken = default)
    {
        return await _context.TrackedGames
            .AsNoTracking()
            .FirstOrDefaultAsync(tg => tg.AppId == appId, cancellationToken);
    }

    public async Task<IReadOnlyList<TrackedGame>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TrackedGames
            .AsNoTracking()
            .Where(tg => tg.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(TrackedGame trackedGame, CancellationToken cancellationToken = default)
    {
        var existing = await _context.TrackedGames.FindAsync(new object[] { trackedGame.AppId }, cancellationToken);
        if (existing is null)
        {
            _context.TrackedGames.Add(trackedGame);
        }
        else
        {
            _context.Entry(existing).State = EntityState.Detached;
            _context.Entry(trackedGame).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
