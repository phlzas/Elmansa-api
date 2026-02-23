using Elmansa_api.Models;

namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Repository interface for Lesson entity
    /// </summary>
    public interface ILessonRepository : IGenericRepository<Lesson>
    {
        /// <summary>
        /// Get all lessons in a course
        /// </summary>
        Task<IQueryable<Lesson>> GetLessonsByCourseAsync(int courseId);

        /// <summary>
        /// Get lesson with progress data
        /// </summary>
        Task<Lesson?> GetLessonWithProgressAsync(int lessonId);

        /// <summary>
        /// Get lessons ordered by sequence
        /// </summary>
        Task<IQueryable<Lesson>> GetLessonsOrderedAsync(int courseId);
    }
}
