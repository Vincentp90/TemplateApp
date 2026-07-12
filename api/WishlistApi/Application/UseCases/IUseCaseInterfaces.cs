using Application.Contracts;
using Application.UseCases.AppListing.Requests;
using Application.UseCases.Auction.Requests;
using Application.UseCases.Auth.Requests;
using Application.UseCases.User.Requests;
using Application.UseCases.Wishlist.Requests;

namespace Application.UseCases.Auth
{
    public interface IRegisterUserUseCase
    {
        Task ExecuteAsync(RegisterUserRequest request);
    }

    public interface ILoginUserUseCase
    {
        Task<LoginResult?> ExecuteAsync(LoginUserRequest request);
    }
}

namespace Application.UseCases.Wishlist
{
    public interface IGetWishlistUseCase
    {
        Task<IReadOnlyList<Domain.WishlistItem>> ExecuteAsync(GetWishlistRequest request);
    }

    public interface IAddWishlistItemUseCase
    {
        Task ExecuteAsync(AddWishlistItemRequest request);
    }

    public interface IDeleteWishlistItemUseCase
    {
        Task ExecuteAsync(DeleteWishlistItemRequest request);
    }

    public interface IGetWishlistStatsUseCase
    {
        Task<Domain.WishlistStats> ExecuteAsync(GetWishlistStatsRequest request);
    }

    public interface IPublishBackfillEventUseCase
    {
        Task ExecuteAsync(PublishBackfillEventRequest request);
    }

    public interface ISetAlertRuleUseCase
    {
        Task ExecuteAsync(SetAlertRuleRequest request);
    }

    public interface IDeleteAlertRuleUseCase
    {
        Task ExecuteAsync(DeleteAlertRuleRequest request);
    }
}

namespace Application.UseCases.User
{
    public interface IGetUserProfileUseCase
    {
        Task<Domain.User> ExecuteAsync(GetUserProfileRequest request);
    }

    public interface IUpdateUserProfileUseCase
    {
        Task ExecuteAsync(UpdateUserProfileRequest request);
    }

    public interface IGetPaginatedUsersUseCase
    {
        Task<IReadOnlyList<UserSummaryDto>> ExecuteAsync(GetPaginatedUsersRequest request);
    }
}

namespace Application.UseCases.AppListing
{
    public interface ISearchAppListingsUseCase
    {
        Task<IReadOnlyList<AppListingDto>> ExecuteAsync(SearchAppListingsRequest request);
    }

    public interface IGetRandomAppListingUseCase
    {
        Task<Domain.AppListing> ExecuteAsync(UnitRequest request);
    }

    public interface IEnsureAppListingsPopulatedUseCase
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}

namespace Application.UseCases.Auction
{
    public interface IPlaceBidUseCase
    {
        Task ExecuteAsync(PlaceBidRequest request);
    }

    public interface IStartNextAuctionUseCase
    {
        Task ExecuteAsync(StartNextAuctionRequest request);
    }

    public interface ISimulateBidUseCase
    {
        Task ExecuteAsync();
    }
}
