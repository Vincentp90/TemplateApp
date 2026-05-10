using DataAccess.Users;
using DataAccess.Wishlist;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application
{
    public interface IUserService
    {
        Task<int> GetInternalUserIdAsync(Guid guid);
        //Task<User> GetUserAsync(int id);
        Task<List<User>> GetUsersAsync(int page, int limit);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task<UserDetails> GetUserDetailsAsync(int userId);
        Task UpdateUserDetailsAsync(UserDetails userDetails);
    }

    public class UserService : IUserService
    {
        private readonly IUserDA _userDA;

        public UserService(IUserDA UserDA)
        {
            _userDA = UserDA;
        }

        public async Task<int> GetInternalUserIdAsync(Guid guid)
        {
            return await _userDA.GetInternalUserIdAsync(guid);
        }

        public async Task<UserDetails> GetUserDetailsAsync(int userId)
        {
            return await _userDA.GetUserDetailsAsync(userId);
        }

        public async Task<List<User>> GetUsersAsync(int page, int limit)
        {
            return await _userDA.GetUsersAsync(page, limit);
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            return await _userDA.IsUsernameAvailableAsync(username);
        }

        public async Task UpdateUserDetailsAsync(UserDetails userDetails)
        {
            await _userDA.UpdateUserDetailsAsync(userDetails);
        }
    }
}
