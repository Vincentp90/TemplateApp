using Application.Commands;
using Application.UseCases.Auction.Requests;
using Application.UseCases.Auth;
using Application.UseCases.Auth.Requests;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;

namespace Application.UseCases.Auction;

/// <summary>
/// Use case: simulate a bid by a bot user.
/// Composes LoginUserUseCase + PlaceBidUseCase.
/// </summary>
public class SimulateBidUseCase(
    IAuctionRepository repository,
    IUnitOfWork unitOfWork,
    ILoginUserUseCase loginUserUseCase,
    IPlaceBidUseCase placeBidUseCase,
    IUserRepository userRepo,
    IConfiguration config)
    : ISimulateBidUseCase
{
    public async Task ExecuteAsync()
    {
        var loginResult = await GetSimulationUser();
        var auction = await repository.GetLatestAuctionAsync();
        if (auction == null)
            throw new Domain.Exceptions.NotFoundException("No auction found for simulation.");

        var newPrice = (auction.CurrentPrice ?? auction.StartingPrice) + 10.0M;
        int internalUserId = await GetInternalUserIdAsync(loginResult.UserId);
        await placeBidUseCase.ExecuteAsync(new PlaceBidRequest(
            AuctionId: auction.Id,
            Amount: newPrice,
            UserId: internalUserId,
            RowVersion: auction.RowVersion));
    }

    private async Task<LoginResult> GetSimulationUser()
    {
        const string username = "SimulateAuctionUser";
        string password = config["SimUserPassword"] ?? throw new InvalidOperationException("SimUserPassword configuration is missing.");
        var loginResult = await loginUserUseCase.ExecuteAsync(new LoginUserRequest(username, password));
        if (loginResult == null)
        {
            // Register the simulation user
            var registerUseCase = new RegisterUserUseCase(userRepo, unitOfWork);
            await registerUseCase.ExecuteAsync(new Application.UseCases.Auth.Requests.RegisterUserRequest(username, password));
            loginResult = await loginUserUseCase.ExecuteAsync(new LoginUserRequest(username, password));
        }
        return loginResult!;
    }

    private async Task<int> GetInternalUserIdAsync(Guid externalUserId)
    {
        return await userRepo.GetInternalUserIdAsync(externalUserId);
    }
}
