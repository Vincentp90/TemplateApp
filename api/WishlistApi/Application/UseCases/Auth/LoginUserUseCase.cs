using Application.Commands;
using Application.UseCases.Auth.Requests;
using Domain.Repositories;

namespace Application.UseCases.Auth;

/// <summary>
/// Use case: authenticate a user and return login result.
/// </summary>
public class LoginUserUseCase(IUserRepository userRepo) : ILoginUserUseCase
{
    public async Task<LoginResult?> ExecuteAsync(LoginUserRequest request)
    {
        var user = await userRepo.GetUserAsync(request.Username);

        if (user == null)
            return null;

        if (!PasswordHelper.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            return null;

        return new LoginResult(user.UUID, user.Username, user.Role);
    }
}
