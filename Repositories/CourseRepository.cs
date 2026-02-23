using Elmansa_api.Data;
using Elmansa_api.Models;
using Microsoft.EntityFrameworkCore;

namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Repository for Course entity.
    /// Provides course-specific data access operations.
    /// </summary>
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
        public CourseRepository(
            AppDbContext dbContext,
            ILogger<CourseRepository> logger) 
            : base(dbContext, logger)
        {
        }

        /// <summary>
        /// Get all published courses
        /// </summary>
        public async Task<IQueryable<Course>> GetPublishedCoursesAsync()
        {
            _logger.LogInformation("Getting all published courses");
            return await Task.FromResult(
                _dbSet.Where(c => c.IsPublished)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
            );
        }

        /// <summary>
        /// Get courses by category
        /// </summary>
        public async Task<IQueryable<Course>> GetCoursesByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be empty", nameof(category));

            _logger.LogInformation("Getting courses by category: {Category}", category);
            return await Task.FromResult(
                _dbSet.Where(c => c.Category == category && c.IsPublished)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
            );
        }

        /// <summary>
        /// Get courses created by a specific user
        /// </summary>
        public async Task<IQueryable<Course>> GetCoursesByCreatorAsync(string createdByUserId)
        {
            if (string.IsNullOrWhiteSpace(createdByUserId))
                throw new ArgumentException("User ID cannot be empty", nameof(createdByUserId));

            _logger.LogInformation("Getting courses by creator: {UserId}", createdByUserId);
            return await Task.FromResult(
                _dbSet.Where(c => c.CreatedByUserId == createdByUserId)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
            );
        }

        /// <summary>
        /// Get course with all related data
        /// </summary>
        public async Task<Course?> GetCourseWithDetailsAsync(int courseId)
        {
            _logger.LogInformation("Getting course {CourseId} with details", courseId);
            return await _dbSet
                .Include(c => c.Lessons.OrderBy(l => l.Order))
                .Include(c => c.Enrollments)
                .Include(c => c.CreatedByUser)
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

        /// <summary>
        /// Search courses by title or description
        /// </summary>
        public async Task<IQueryable<Course>> SearchCoursesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

            var lowerSearchTerm = searchTerm.ToLower();
            _logger.LogInformation("Searching courses with term: {SearchTerm}", searchTerm);
            
            return await Task.FromResult(
                _dbSet.Where(c => 
                    c.IsPublished && (
                        c.Title.ToLower().Contains(lowerSearchTerm) ||
                        c.Description.ToLower().Contains(lowerSearchTerm) ||
                        c.Category.ToLower().Contains(lowerSearchTerm)
                    ))
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
            );
        }

        /// <summary>
        /// Get popular courses by enrollment count
        /// </summary>
        public async Task<IQueryable<Course>> GetPopularCoursesAsync(int take = 10)
        {
            _logger.LogInformation("Getting {Count} popular courses", take);
            return await Task.FromResult(
                _dbSet.Where(c => c.IsPublished)
                    .Include(c => c.Enrollments)
                    .OrderByDescending(c => c.Enrollments.Count)
                    .Take(take)
                    .AsNoTracking()
            );
        }

        /// <summary>
        /// Get courses by difficulty level
        /// </summary>
        public async Task<IQueryable<Course>> GetCoursesByLevelAsync(string level)
        {
            if (string.IsNullOrWhiteSpace(level))
                throw new ArgumentException("Level cannot be empty", nameof(level));

            _logger.LogInformation("Getting courses by level: {Level}", level);
            return await Task.FromResult(
                _dbSet.Where(c => c.Level == level && c.IsPublished)
                    .OrderByDescending(c => c.CreatedAt)
                    .AsNoTracking()
            );
        }
    }
}
