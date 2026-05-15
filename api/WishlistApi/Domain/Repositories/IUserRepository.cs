using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserAsync(string Username);
        void AddUserAsync(User user);
    }
}
