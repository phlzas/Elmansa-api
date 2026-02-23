using Elmansa_api.Data;
using Elmansa_api.Models;
using Microsoft.EntityFrameworkCore;

namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Repository for Lesson entity
    /// </summary>
    public class LessonRepository : GenericRepository<Lesson>, ILessonRepository
    {
        public LessonRepository(
            AppDbContext dbContext,
            ILogger<LessonRepository> logger)
            : base(dbContext, logger)
        {
        }

        public async Task<IQueryable<Lesson>> GetLessonsByCourseAsync(int courseId)
        {
            _logger.LogInformation("Getting lessons for course: {CourseId}", courseId);
            return await Task.FromResult(
                _dbSet.Where(l => l.CourseId == courseId)
                    .Include(l => l.Course)
                    .OrderBy(l => l.Order)
                    .AsNoTracking()
            );
        }

        public async Task<Lesson?> GetLessonWithProgressAsync(int lessonId)
        {
            _logger.LogInformation("Getting lesson {LessonId} with progress", lessonId);
            return await _dbSet
                .Include(l => l.Course)
                .Include(l => l.Progress)
                .FirstOrDefaultAsync(l => l.Id == lessonId);
        }

        public async Task<IQueryable<Lesson>> GetLessonsOrderedAsync(int courseId)
        {
            _logger.LogInformation("Getting ordered lessons for course: {CourseId}", courseId);
            return await Task.FromResult(
                _dbSet.Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.Order)
                    .AsNoTracking()
            );
        }
    }
}
