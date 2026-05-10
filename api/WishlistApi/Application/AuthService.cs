using DataAccess.Users;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Application
{
    public interface IAuthService
    {
        Task<User?> LoginAsync(string username, string password);
        Task AddUserAsync(string username, string password);
    }

    public class AuthService(IUserDA userDA) : IAuthService
    {
        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await userDA.GetUserAsync(username);

            if (user == null)
                return null;
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            return user;
        }

        //TODO here in Auth or in UserService? Where does it fit best?
        public async Task AddUserAsync(string username, string password)
        {
            CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            var user = new User
            {
                Username = username,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            await userDA.AddUserAsync(user);
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

    // TODO move this somewhere suitable
    public class LoginResult
    {
        // TODO this shoudn't be here, it's to make AuctionService.SimulateBid work for now
        public int UserID { get; set; }


        public string Token { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
        public string Error { get; set; } = string.Empty;

        internal static LoginResult Failed()
        {
            return new LoginResult { Success = false, Error = "Invalid username or password" };
        }

        internal static LoginResult SuccessResult(string token, int userID)
        {
            return new LoginResult { Success = true, Token = token, UserID = userID };
        }
    }
}
