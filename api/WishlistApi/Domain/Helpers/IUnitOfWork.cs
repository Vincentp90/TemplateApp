using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Helpers
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
