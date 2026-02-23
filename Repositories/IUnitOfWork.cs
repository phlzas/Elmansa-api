namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Unit of Work interface
    /// Manages all repositories and coordinates database transactions
    /// </summary>
    public interface IUnitOfWork : IAsyncDisposable
    {
        /// <summary>
        /// Course repository
        /// </summary>
        ICourseRepository Courses { get; }

        /// <summary>
        /// Enrollment repository
        /// </summary>
        IEnrollmentRepository Enrollments { get; }

        /// <summary>
        /// Lesson repository
        /// </summary>
        ILessonRepository Lessons { get; }

        /// <summary>
        /// Generic repository for any entity
        /// </summary>
        IGenericRepository<T> GetRepository<T>() where T : class;

        /// <summary>
        /// Save all changes in a single transaction
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Begin a transaction
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commit the current transaction
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        Task RollbackAsync();
    }
}
