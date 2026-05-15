using Application.Commands;
using DataAccess.Users;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application
{
    public interface IAuctionService
    {
        Task PlaceBidAsync(PlaceBidCommand command);
        Task StartNextAuctionAsync();
        Task SimulateBid();
    }

    public class AuctionService(IAuctionRepository repository, IUnitOfWork unitOfWork, IAuthService authService, IAppListingService appListingService, IConfiguration config, IUserDA userDA) : IAuctionService
    {
        public async Task StartNextAuctionAsync()
        {
            var app = await appListingService.GetRandomAppListingAsync();

            var newAuction = new Domain.Auction()
            {
                DateAdded = DateTimeOffset.UtcNow,
                Status = AuctionStatus.Open,
                RowVersion = 0,
                AppListingId = app.appid,
                StartingPrice = 1.0m,
            };

            var latestAuction = await repository.GetLatestAuctionAsync();
            if (latestAuction != null)
            {
                await repository.CloseAuctionAndAddNewAsync(newAuction);
            }
            else
            {
                repository.AddAuction(newAuction);
            }
            await unitOfWork.SaveChangesAsync();
        }

        public async Task PlaceBidAsync(PlaceBidCommand command)
        {
            var auction = await repository.GetOpenAuction(command.AuctionId);

            if (auction == null)
                throw new NotFoundException("Auction not found.");

            auction.PlaceBid(command.UserId, command.Amount);

            repository.Update(auction, command.RowVersion);

            await unitOfWork.SaveChangesAsync();
        }

        public async Task SimulateBid()
        {
            var loginResult = await GetSimulationUser();
            Auction auction = (await repository.GetLatestAuctionAsync())!;
            var newPrice = (auction.CurrentPrice ?? auction.StartingPrice) + 10.0M;
            var userId = await userDA.GetInternalUserIdAsync(loginResult.UserId);
            await PlaceBidAsync(new PlaceBidCommand(AuctionId: auction.Id, Amount: newPrice, UserId: userId, RowVersion: auction.RowVersion ));
        }

        private async Task<LoginResult> GetSimulationUser()
        {
            const string username = "SimulateAuctionUser";
            string password = config["SimUserPassword"]!;
            var command = new LoginCommand(username, password);
            var loginResult = await authService.LoginAsync(command);
            if (loginResult == null)
            {
                await authService.AddUserAsync(new RegisterUserCommand(username, password));
                loginResult = await authService.LoginAsync(command);
            }
            return loginResult!;
        }
    }
}
