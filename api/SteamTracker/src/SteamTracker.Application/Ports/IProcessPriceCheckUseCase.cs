using System.Threading;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Called by PriceCheckWorker after fetching price from Steam.
/// Saves the price, evaluates alert rules, and dispatches notifications.
/// </summary>
public interface IProcessPriceCheckUseCase
{
    Task ExecuteAsync(int appId, Money price, string name, CancellationToken cancellationToken = default);
}
