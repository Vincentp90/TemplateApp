using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Helpers
{
    // TODO not fully DDD yet
    public interface IUnitOfWork
    {
        Task SaveChangesAsync();
    }
}
