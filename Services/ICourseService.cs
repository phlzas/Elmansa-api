using Elmansa_api.RequestResponse;

namespace Elmansa_api.Services
{
    /// <summary>
    /// Service interface for course operations
    /// Contains business logic for courses
    /// </summary>
    public interface ICourseService
    {
        /// <summary>
        /// Get all courses with pagination
        /// </summary>
        Task<(List<CourseDto> courses, int total)> GetAllCoursesAsync(int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Get published courses only
        /// </summary>
        Task<(List<CourseDto> courses, int total)> GetPublishedCoursesAsync(int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Get courses by category
        /// </summary>
        Task<(List<CourseDto> courses, int total)> GetCoursesByCategoryAsync(
            string category, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Get course by ID with details
        /// </summary>
        Task<CourseDetailDto?> GetCourseDetailAsync(int courseId);

        /// <summary>
        /// Search courses
        /// </summary>
        Task<(List<CourseDto> courses, int total)> SearchCoursesAsync(
            string searchTerm, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Get popular courses
        /// </summary>
        Task<List<CourseDto>> GetPopularCoursesAsync(int take = 10);

        /// <summary>
        /// Create new course
        /// </summary>
        Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, string createdByUserId);

        /// <summary>
        /// Update course
        /// </summary>
        Task<CourseDto?> UpdateCourseAsync(UpdateCourseDto dto, string userId);

        /// <summary>
        /// Delete course
        /// </summary>
        Task<bool> DeleteCourseAsync(int courseId, string userId);

        /// <summary>
        /// Publish course
        /// </summary>
        Task<bool> PublishCourseAsync(int courseId, string userId);

        /// <summary>
        /// Get courses created by user
        /// </summary>
        Task<List<CourseDto>> GetMyCoursesAsync(string userId);
    }
}
