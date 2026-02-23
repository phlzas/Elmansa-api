using Elmansa_api.Data;
using Elmansa_api.Models;
using Microsoft.EntityFrameworkCore;

namespace Elmansa_api.Repositories
{
    /// <summary>
    /// Repository for Enrollment entity
    /// </summary>
    public class EnrollmentRepository : GenericRepository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(
            AppDbContext dbContext,
            ILogger<EnrollmentRepository> logger)
            : base(dbContext, logger)
        {
        }

        public async Task<IQueryable<Enrollment>> GetStudentEnrollmentsAsync(string studentUserId)
        {
            if (string.IsNullOrWhiteSpace(studentUserId))
                throw new ArgumentException("Student user ID cannot be empty", nameof(studentUserId));

            _logger.LogInformation("Getting enrollments for student: {StudentUserId}", studentUserId);
            return await Task.FromResult(
                _dbSet.Where(e => e.StudentUserId == studentUserId)
                    .Include(e => e.Course)
                    .OrderByDescending(e => e.EnrolledAt)
                    .AsNoTracking()
            );
        }

        public async Task<IQueryable<Enrollment>> GetCourseEnrollmentsAsync(int courseId)
        {
            _logger.LogInformation("Getting enrollments for course: {CourseId}", courseId);
            return await Task.FromResult(
                _dbSet.Where(e => e.CourseId == courseId)
                    .Include(e => e.StudentUser)
                    .OrderByDescending(e => e.EnrolledAt)
                    .AsNoTracking()
            );
        }

        public async Task<Enrollment?> GetEnrollmentWithDetailsAsync(int enrollmentId)
        {
            _logger.LogInformation("Getting enrollment {EnrollmentId} with details", enrollmentId);
            return await _dbSet
                .Include(e => e.StudentUser)
                .Include(e => e.Course)
                .ThenInclude(c => c.Lessons)
                .Include(e => e.LessonProgress)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);
        }

        public async Task<bool> IsStudentEnrolledAsync(string studentUserId, int courseId)
        {
            if (string.IsNullOrWhiteSpace(studentUserId))
                throw new ArgumentException("Student user ID cannot be empty", nameof(studentUserId));

            return await _dbSet.AnyAsync(e => 
                e.StudentUserId == studentUserId && 
                e.CourseId == courseId &&
                e.Status == EnrollmentStatus.Active);
        }

        public async Task<IQueryable<Enrollment>> GetActiveEnrollmentsAsync()
        {
            _logger.LogInformation("Getting active enrollments");
            return await Task.FromResult(
                _dbSet.Where(e => e.Status == EnrollmentStatus.Active)
                    .Include(e => e.StudentUser)
                    .Include(e => e.Course)
                    .AsNoTracking()
            );
        }
    }
}
