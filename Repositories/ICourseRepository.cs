using Elmansa_api.Models;

namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Repository interface for Course entity.
    /// Extends generic repository with course-specific operations.
    /// </summary>
    public interface ICourseRepository : IGenericRepository<Course>
    {
        /// <summary>
        /// Get all published courses
        /// </summary>
        Task<IQueryable<Course>> GetPublishedCoursesAsync();

        /// <summary>
        /// Get courses by category
        /// </summary>
        Task<IQueryable<Course>> GetCoursesByCategoryAsync(string category);

        /// <summary>
        /// Get courses created by a specific user
        /// </summary>
        Task<IQueryable<Course>> GetCoursesByCreatorAsync(string createdByUserId);

        /// <summary>
        /// Get course with all related data (lessons, enrollments)
        /// </summary>
        Task<Course?> GetCourseWithDetailsAsync(int courseId);

        /// <summary>
        /// Search courses by title or description
        /// </summary>
        Task<IQueryable<Course>> SearchCoursesAsync(string searchTerm);

        /// <summary>
        /// Get popular courses (by enrollment count)
        /// </summary>
        Task<IQueryable<Course>> GetPopularCoursesAsync(int take = 10);

        /// <summary>
        /// Get courses by difficulty level
        /// </summary>
        Task<IQueryable<Course>> GetCoursesByLevelAsync(string level);
    }
}
