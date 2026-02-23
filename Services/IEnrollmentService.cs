using Elmansa_api.RequestResponse;

namespace Elmansa_api.Services
{
    /// <summary>
    /// Service interface for enrollment operations
    /// </summary>
    public interface IEnrollmentService
    {
        /// <summary>
        /// Enroll student in course
        /// </summary>
        Task<EnrollmentDto> EnrollStudentAsync(string studentUserId, int courseId);

        /// <summary>
        /// Get student enrollments
        /// </summary>
        Task<List<EnrollmentDto>> GetStudentEnrollmentsAsync(string studentUserId);

        /// <summary>
        /// Get course enrollments
        /// </summary>
        Task<(List<EnrollmentDto> enrollments, int total)> GetCourseEnrollmentsAsync(
            int courseId, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Get enrollment details
        /// </summary>
        Task<EnrollmentDto?> GetEnrollmentAsync(int enrollmentId);

        /// <summary>
        /// Update lesson progress
        /// </summary>
        Task<LessonProgressDto> UpdateLessonProgressAsync(
            int enrollmentId, int lessonId, int progressPercentage, bool isCompleted);

        /// <summary>
        /// Complete enrollment
        /// </summary>
        Task<bool> CompleteEnrollmentAsync(int enrollmentId, decimal? grade);

        /// <summary>
        /// Drop course
        /// </summary>
        Task<bool> DropCourseAsync(int enrollmentId);

        /// <summary>
        /// Check if student is enrolled
        /// </summary>
        Task<bool> IsEnrolledAsync(string studentUserId, int courseId);
    }
}
