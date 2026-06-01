using Application.Commands;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Application
{
    public interface IAuthService
    {
        Task<LoginResult?> LoginAsync(LoginCommand command);
        Task AddUserAsync(RegisterUserCommand command);
    }

    public class AuthService(IUserRepository userRepo, IUnitOfWork unitOfWork) : IAuthService
    {
        public async Task<LoginResult?> LoginAsync(LoginCommand command)
        {
            var user = await userRepo.GetUserAsync(command.Username);

            if (user == null)
                return null;
            if (!VerifyPasswordHash(command.Password, user.PasswordHash, user.PasswordSalt))
                return null;

            return new LoginResult(user.UUID, user.Username, user.Role);
        }

        public async Task AddUserAsync(RegisterUserCommand command)
        {
            if (!await userRepo.IsUsernameAvailableAsync(command.Username))
                throw new DomainException("Username already taken");

            CreatePasswordHash(command.Password, out byte[] hash, out byte[] salt);

            var user = new Domain.User(
                username: command.Username,
                passwordHash: hash,
                passwordSalt: salt
            );

            userRepo.AddUser(user);
            await unitOfWork.SaveChangesAsync();
        }

        private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            salt = RandomNumberGenerator.GetBytes(16);
            hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations: 100_000, HashAlgorithmName.SHA256, outputLength: 32);
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            var computed = Rfc2898DeriveBytes.Pbkdf2(password, storedSalt, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(computed, storedHash);
        }
    }
}
