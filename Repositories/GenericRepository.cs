using Elmansa_api.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Generic repository implementation for CRUD operations.
    /// Reduces code duplication by providing common functionality.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _dbContext;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<GenericRepository<T>> _logger;

        public GenericRepository(
            AppDbContext dbContext,
            ILogger<GenericRepository<T>> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbSet = _dbContext.Set<T>();
        }

        /// <summary>
        /// Get all entities (returns IQueryable for composition)
        /// </summary>
        public async Task<IQueryable<T>> GetAllAsync()
        {
            _logger.LogInformation("Getting all entities of type {EntityType}", typeof(T).Name);
            return await Task.FromResult(_dbSet.AsNoTracking());
        }

        /// <summary>
        /// Get entity by ID
        /// </summary>
        public async Task<T?> GetByIdAsync(object id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            _logger.LogInformation("Getting entity of type {EntityType} by ID: {Id}", typeof(T).Name, id);
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Find entities matching a predicate
        /// </summary>
        public async Task<IQueryable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _logger.LogInformation("Finding entities of type {EntityType} with predicate", typeof(T).Name);
            return await Task.FromResult(_dbSet.Where(predicate).AsNoTracking());
        }

        /// <summary>
        /// Get first entity matching predicate
        /// </summary>
        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _logger.LogInformation("Getting first entity of type {EntityType} matching predicate", typeof(T).Name);
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// Add a new entity
        /// </summary>
        public async Task<T> AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogInformation("Adding entity of type {EntityType}", typeof(T).Name);
            await _dbSet.AddAsync(entity);
            await SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Add multiple entities
        /// </summary>
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("Entities cannot be null or empty", nameof(entities));

            _logger.LogInformation("Adding {Count} entities of type {EntityType}", 
                entities.Count(), typeof(T).Name);
            
            await _dbSet.AddRangeAsync(entities);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Update an existing entity
        /// </summary>
        public async Task<T> UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogInformation("Updating entity of type {EntityType}", typeof(T).Name);
            
            _dbSet.Update(entity);
            await SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Delete entity by ID
        /// </summary>
        public async Task<bool> DeleteAsync(object id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            _logger.LogInformation("Deleting entity of type {EntityType} by ID: {Id}", typeof(T).Name, id);
            
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Entity of type {EntityType} with ID {Id} not found", typeof(T).Name, id);
                return false;
            }

            return await DeleteAsync(entity);
        }

        /// <summary>
        /// Delete an entity
        /// </summary>
        public async Task<bool> DeleteAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                _logger.LogInformation("Deleting entity of type {EntityType}", typeof(T).Name);
                _dbSet.Remove(entity);
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity of type {EntityType}", typeof(T).Name);
                return false;
            }
        }

        /// <summary>
        /// Delete multiple entities
        /// </summary>
        public async Task<bool> DeleteRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("Entities cannot be null or empty", nameof(entities));

            try
            {
                _logger.LogInformation("Deleting {Count} entities of type {EntityType}", 
                    entities.Count(), typeof(T).Name);
                
                _dbSet.RemoveRange(entities);
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entities of type {EntityType}", typeof(T).Name);
                return false;
            }
        }

        /// <summary>
        /// Check if entity exists
        /// </summary>
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// Count entities
        /// </summary>
        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();

            return await _dbSet.CountAsync(predicate);
        }

        /// <summary>
        /// Save changes to database
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error occurred");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving changes");
                throw;
            }
        }

        /// <summary>
        /// Get queryable for advanced scenarios
        /// </summary>
        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }
    }
}
