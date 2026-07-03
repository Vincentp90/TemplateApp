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
    private readonly string? _connectionString;

    public SharedDbPriceReader(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SteamTrackerConnection");
    }

    public async Task<Dictionary<int, GamePrice>> GetPricesAsync(IEnumerable<int> appIds)
    {
        if (string.IsNullOrEmpty(_connectionString))
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
            .ToDictionary(r => r.AppId, r => new GamePrice(r.CurrentPriceAmount, r.CurrentPriceCurrency ?? "EUR", r.LastCheckedAt));
    }

    public async Task<Dictionary<int, AlertRuleInfo>> GetAlertRulesAsync(string userId)
    {
        if (string.IsNullOrEmpty(_connectionString))
            return new Dictionary<int, AlertRuleInfo>();

        using var connection = new NpgsqlConnection(_connectionString);

        const string sql = @"
            SELECT ""AlertRuleId"", ""AppId"", ""TriggerBelowPrice"", ""Currency""
            FROM ""AlertRules""
            WHERE ""UserId"" = @UserId AND ""IsActive"" = true";

        var rows = await connection.QueryAsync<AlertRuleRow>(sql, new { UserId = userId });

        return rows
            .Where(r => r.AppId > 0)
            .ToDictionary(
                r => r.AppId,
                r => new AlertRuleInfo(r.AlertRuleId, r.TriggerBelowPrice, r.Currency));
    }

    private record GamePriceRow(int AppId, decimal? CurrentPriceAmount, string? CurrentPriceCurrency, DateTimeOffset? LastCheckedAt);
    private record AlertRuleRow(Guid AlertRuleId, int AppId, decimal TriggerBelowPrice, string Currency);
}
