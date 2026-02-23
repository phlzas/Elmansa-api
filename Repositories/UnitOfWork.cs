using Elmansa_api.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Unit of Work implementation
    /// Manages all repositories and transactions
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly IServiceProvider _serviceProvider;
        private IDbContextTransaction? _transaction;

        private ICourseRepository? _courseRepository;
        private IEnrollmentRepository? _enrollmentRepository;
        private ILessonRepository? _lessonRepository;

        private readonly Dictionary<Type, object> _repositories = new();

        public UnitOfWork(
            AppDbContext dbContext,
            ILogger<UnitOfWork> logger,
            IServiceProvider serviceProvider)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Get or create course repository
        /// </summary>
        public ICourseRepository Courses =>
            _courseRepository ??= new CourseRepository(_dbContext, 
                _serviceProvider.GetRequiredService<ILogger<CourseRepository>>());

        /// <summary>
        /// Get or create enrollment repository
        /// </summary>
        public IEnrollmentRepository Enrollments =>
            _enrollmentRepository ??= new EnrollmentRepository(_dbContext,
                _serviceProvider.GetRequiredService<ILogger<EnrollmentRepository>>());

        /// <summary>
        /// Get or create lesson repository
        /// </summary>
        public ILessonRepository Lessons =>
            _lessonRepository ??= new LessonRepository(_dbContext,
                _serviceProvider.GetRequiredService<ILogger<LessonRepository>>());

        /// <summary>
        /// Get or create generic repository for any entity type
        /// </summary>
        public IGenericRepository<T> GetRepository<T>() where T : class
        {
            var type = typeof(T);
            
            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>).MakeGenericType(type);
                var loggerType = typeof(ILogger<>).MakeGenericType(repositoryType);
                var logger = _serviceProvider.GetService(loggerType) ?? 
                    _serviceProvider.GetRequiredService<ILoggerFactory>()
                        .CreateLogger(repositoryType.Name);

                var repositoryInstance = Activator.CreateInstance(repositoryType, _dbContext, logger)
                    ?? throw new InvalidOperationException($"Cannot create repository for type {type.Name}");

                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<T>)_repositories[type];
        }

        /// <summary>
        /// Save all changes to database
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                _logger.LogInformation("Saving changes to database");
                return await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        /// <summary>
        /// Begin a transaction
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync();
            _logger.LogInformation("Transaction started");
        }

        /// <summary>
        /// Commit the current transaction
        /// </summary>
        public async Task CommitAsync()
        {
            try
            {
                await SaveChangesAsync();
                if (_transaction != null)
                    await _transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing transaction");
                await RollbackAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        public async Task RollbackAsync()
        {
            try
            {
                if (_transaction != null)
                    await _transaction.RollbackAsync();
                _logger.LogInformation("Transaction rolled back");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back transaction");
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();
            await _dbContext.DisposeAsync();
            _logger.LogInformation("UnitOfWork disposed");
        }
    }
}
