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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Application
{
    public interface IAuctionService
    {
        Task<Auction?> GetLatestAuctionAsync();
        Task PlaceBidAsync(PlaceBidCommand command);
        Task StartNextAuctionAsync();
        Task SimulateBid();
    }

    public class AuctionService(IAuctionRepository repository, IUnitOfWork unitOfWork, IAuthService authService, IAppListingService appListingService, IConfiguration config) : IAuctionService
    {
        public async Task StartNextAuctionAsync()
        {
            var app = await appListingService.GetRandomAppListingAsync();

            var newAuction = new Domain.Auction()
            {
                DateAdded = DateTimeOffset.UtcNow,
                Status = AuctionStatus.Open,
                RowVersion = 0,
                appid = app.appid,
                StartingPrice = 1.0m,
            };

            var latestAuction = await GetLatestAuctionAsync();
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

        public async Task<Domain.Auction?> GetLatestAuctionAsync()
        {
            return await repository.GetLatestAuctionAsync();
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
            var user = await GetSimulationUser();
            Auction auction = (await GetLatestAuctionAsync())!;
            var newPrice = (auction.CurrentPrice ?? auction.StartingPrice) + 10.0M;
            await PlaceBidAsync(new PlaceBidCommand(AuctionId: auction.Id, Amount: newPrice, UserId: user.ID, RowVersion: auction.RowVersion ));
        }

        private async Task<User> GetSimulationUser()
        {
            const string username = "SimulateAuctionUser";
            string password = config["SimUserPassword"]!;
            var user = await authService.LoginAsync(username, password);
            if (user == null)
            {
                await authService.AddUserAsync(username, password);
                user = await authService.LoginAsync(username, password);
            }
            return user!;
        }
    }
}
