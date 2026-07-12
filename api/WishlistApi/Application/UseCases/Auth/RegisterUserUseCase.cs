using Application.UseCases.Auth.Requests;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;

namespace Application.UseCases.Auth;

/// <summary>
/// Use case: register a new user.
/// </summary>
public class RegisterUserUseCase(
    IUserRepository userRepo,
    IUnitOfWork unitOfWork)
    : IRegisterUserUseCase
{
    public async Task ExecuteAsync(RegisterUserRequest request)
    {
        if (!await userRepo.IsUsernameAvailableAsync(request.Username))
            throw new DomainException("Username already taken");

        PasswordHelper.CreatePasswordHash(request.Password, out byte[] hash, out byte[] salt);

        var user = new Domain.User(
            username: request.Username,
            passwordHash: hash,
            passwordSalt: salt);

        userRepo.AddUser(user);
        await unitOfWork.SaveChangesAsync();
    }
}
