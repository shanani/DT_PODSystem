using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DT_PODSystem.Areas.Security.Repositories.Interfaces
{
    /// <summary>
    /// Security-specific repository interface - complete with all methods used by services
    /// </summary>
    public interface ISecurityRepository<T> where T : class
    {
        // Basic CRUD operations
        Task<T> GetByIdAsync(int id);
        Task<T> GetFirstAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> predicate);

        // Queryable operations (CRITICAL - used extensively by services)
        Task<IQueryable<T>> GetQueryableAsync();
        IQueryable<T> GetQueryable(); // Synchronous version also needed

        // Add operations
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

        // Update operations (CRITICAL - missing methods)
        Task<T> UpdateAsync(T entity);
        T Update(T entity); // Synchronous version needed by services

        // Delete operations
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteAsync(T entity);

        // MISSING: Remove operations (CRITICAL - services use these)
        void Remove(T entity); // Synchronous remove - MISSING!
        void RemoveRange(IEnumerable<T> entities); // Remove multiple entities - MISSING!

        // Query operations
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        // EF Core extension methods support (CRITICAL for LINQ operations)
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<List<T>> ToListAsync();
    }
}