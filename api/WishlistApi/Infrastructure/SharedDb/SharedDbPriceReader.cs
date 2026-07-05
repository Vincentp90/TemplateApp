using System.Data;
using Application.Contracts;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Infrastructure.SharedDb;

/// <summary>
/// Dapper-based reader for SteamTracker's database.
/// Reads prices from the games table and alert rules from the alert_rules table.
/// Uses PascalCase column names to match SteamTracker's EF Core conventions.
/// </summary>
public class SharedDbPriceReader : ISharedDbPriceReader
{
    private readonly string _connectionString;

    public SharedDbPriceReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SteamTrackerConnection")!;
    }

    public async Task<Dictionary<int, GamePrice>> GetPricesAsync(IEnumerable<int> appIds)
    {
        if (appIds == null)
            return new Dictionary<int, GamePrice>();

        var ids = appIds.ToList();
        if (!ids.Any())
            return new Dictionary<int, GamePrice>();

        using var connection = new NpgsqlConnection(_connectionString);

        const string sql = @"
            SELECT ""AppId"", ""CurrentPriceAmount"", ""CurrentPriceCurrency"", ""LastCheckedAt""
            FROM ""Games""
            WHERE ""AppId"" = ANY(@Ids)";

        var rows = await connection.QueryAsync<GamePriceRow>(sql, new { Ids = ids });

        return rows
            .Where(r => r.AppId > 0)
            .ToDictionary(r => r.AppId, r => new GamePrice(r.CurrentPriceAmount, r.CurrentPriceCurrency, r.LastCheckedAt));
    }

    public async Task<Dictionary<int, AlertRuleInfo>> GetAlertRulesAsync(string userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        // TriggerBelowPrice is stored as "Amount|Currency" (combined column per AlertRuleConfig)
        const string sql = @"
            SELECT ""AlertRuleId"", ""AppId"", ""TriggerBelowPrice""
            FROM ""AlertRules""
            WHERE ""UserId"" = @UserId AND ""IsActive"" = true";

        var rows = await connection.QueryAsync<AlertRuleRow>(sql, new { UserId = userId });

        return rows
            .Where(r => r.AppId > 0)
            .Select(ParseAlertRule)
            .Where(r => r.HasValue)
            .ToDictionary(
                r => r!.Value.AppId,
                r => r!.Value.Info);
    }

    private static (decimal Amount, string Currency) ParseMoneyString(string value)
    {
        var parts = value.Split('|');
        return parts.Length == 2
            ? (decimal.Parse(parts[0]), parts[1])
            : (0, string.Empty);
    }

    private static (int AppId, AlertRuleInfo Info)? ParseAlertRule(AlertRuleRow row)
    {
        if (row.AppId <= 0) return null;
        var money = ParseMoneyString(row.TriggerBelowPrice);
        if (money.Amount <= 0) return null;
        return (row.AppId, new AlertRuleInfo(row.AlertRuleId, money.Amount, money.Currency));
    }

    private record GamePriceRow(int AppId, decimal? CurrentPriceAmount, string? CurrentPriceCurrency, DateTime? LastCheckedAt);
    private record AlertRuleRow(Guid AlertRuleId, int AppId, string TriggerBelowPrice);
}
