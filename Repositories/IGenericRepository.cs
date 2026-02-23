using System.Linq.Expressions;

namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Generic repository interface for CRUD operations.
    /// Provides common database operations for any entity.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Get all entities with optional filtering and pagination
        /// </summary>
        Task<IQueryable<T>> GetAllAsync();

        /// <summary>
        /// Get entity by ID
        /// </summary>
        Task<T?> GetByIdAsync(object id);

        /// <summary>
        /// Get entities that match the predicate
        /// </summary>
        Task<IQueryable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Get first entity matching the predicate (or null)
        /// </summary>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Add a new entity
        /// </summary>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// Add multiple entities
        /// </summary>
        Task AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Update an existing entity
        /// </summary>
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// Delete an entity by ID
        /// </summary>
        Task<bool> DeleteAsync(object id);

        /// <summary>
        /// Delete an entity
        /// </summary>
        Task<bool> DeleteAsync(T entity);

        /// <summary>
        /// Delete multiple entities
        /// </summary>
        Task<bool> DeleteRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Check if entity exists
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Count entities matching predicate
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        /// <summary>
        /// Save changes to database
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Get queryable for complex queries with includes
        /// </summary>
        IQueryable<T> GetQueryable();
    }
}
