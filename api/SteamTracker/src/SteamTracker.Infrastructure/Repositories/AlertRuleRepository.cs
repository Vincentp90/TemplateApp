using Microsoft.EntityFrameworkCore;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;

namespace SteamTracker.Infrastructure.Repositories;

public class AlertRuleRepository : IAlertRuleRepository
{
    private readonly SteamTrackerDbContext _context;

    public AlertRuleRepository(SteamTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AlertRule>> GetActiveRulesForAsync(SteamAppId appId, CancellationToken cancellationToken = default)
    {
        return await _context.AlertRules
            .AsNoTracking()
            .Where(ar => ar.AppId == appId && ar.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertRule>> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.AlertRules
            .AsNoTracking()
            .Where(ar => ar.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<AlertRule?> GetAsync(Guid alertRuleId, CancellationToken cancellationToken = default)
    {
        return await _context.AlertRules
            .AsNoTracking()
            .FirstOrDefaultAsync(ar => ar.AlertRuleId == alertRuleId, cancellationToken);
    }

    public async Task SaveAsync(AlertRule alertRule, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AlertRules.FirstOrDefaultAsync(ar => ar.AlertRuleId == alertRule.AlertRuleId, cancellationToken);
        if (existing is null)
        {
            _context.AlertRules.Add(alertRule);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(alertRule);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(AlertRule alertRule, CancellationToken cancellationToken = default)
    {
        _context.AlertRules.Remove(alertRule);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
