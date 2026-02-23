using Elmansa_api.Models;

namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Repository interface for Enrollment entity
    /// </summary>
    public interface IEnrollmentRepository : IGenericRepository<Enrollment>
    {
        /// <summary>
        /// Get student enrollments
        /// </summary>
        Task<IQueryable<Enrollment>> GetStudentEnrollmentsAsync(string studentUserId);

        /// <summary>
        /// Get course enrollments
        /// </summary>
        Task<IQueryable<Enrollment>> GetCourseEnrollmentsAsync(int courseId);

        /// <summary>
        /// Get enrollment with related data
        /// </summary>
        Task<Enrollment?> GetEnrollmentWithDetailsAsync(int enrollmentId);

        /// <summary>
        /// Check if student is enrolled in course
        /// </summary>
        Task<bool> IsStudentEnrolledAsync(string studentUserId, int courseId);

        /// <summary>
        /// Get active enrollments
        /// </summary>
        Task<IQueryable<Enrollment>> GetActiveEnrollmentsAsync();
    }
}
