using System;
using System.Threading.Tasks;

namespace DT_PODSystem.Areas.Security.Repositories.Interfaces
{
    public interface ISecurityUnitOfWork : IDisposable
    {
        // FIXED: Use ISecurityRepository instead of IGenericRepository
        ISecurityRepository<T> Repository<T>() where T : class;

        // Transaction management
        Task<int> SaveChangesAsync();
        int SaveChanges();

        // Transaction support
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // Async dispose
        ValueTask DisposeAsync();
    }
}
