using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserAsync(string Username);
        Task<User> GetUserAsync(int userId);
        void AddUser(User user);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task UpdateUserAsync(User user);
        Task<int> GetInternalUserIdAsync(Guid externalUserId);
    }
}
