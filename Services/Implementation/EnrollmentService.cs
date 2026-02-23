using Elmansa_api.Models;
using Elmansa_api.Repositories;
using Elmansa_api.RequestResponse;
using Microsoft.EntityFrameworkCore;

namespace Elmansa_api.Services.Implementation
{
    /// <summary>
    /// Enrollment service implementation
    /// </summary>
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(
            IUnitOfWork unitOfWork,
            ILogger<EnrollmentService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EnrollmentDto> EnrollStudentAsync(string studentUserId, int courseId)
        {
            if (string.IsNullOrWhiteSpace(studentUserId))
                throw new ArgumentException("Student user ID cannot be empty", nameof(studentUserId));

            _logger.LogInformation("Enrolling student {StudentUserId} in course {CourseId}", studentUserId, courseId);

            // Check if already enrolled
            var isEnrolled = await _unitOfWork.Enrollments.IsStudentEnrolledAsync(studentUserId, courseId);
            if (isEnrolled)
                throw new InvalidOperationException("Student is already enrolled in this course");

            // Create enrollment
            var enrollment = new Enrollment
            {
                StudentUserId = studentUserId,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Active,
                CompletionPercentage = 0
            };

            var createdEnrollment = await _unitOfWork.Enrollments.AddAsync(enrollment);

            // Load the Course navigation property so CourseName is populated in the response
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            createdEnrollment.Course = course;

            return MapToEnrollmentDto(createdEnrollment);
        }

        public async Task<List<EnrollmentDto>> GetStudentEnrollmentsAsync(string studentUserId)
        {
            if (string.IsNullOrWhiteSpace(studentUserId))
                throw new ArgumentException("Student user ID cannot be empty", nameof(studentUserId));

            _logger.LogInformation("Getting enrollments for student: {StudentUserId}", studentUserId);

            var query = await _unitOfWork.Enrollments.GetStudentEnrollmentsAsync(studentUserId);
            var enrollmentEntities = await query.ToListAsync();
            var enrollments = enrollmentEntities.Select(e => MapToEnrollmentDto(e)).ToList();

            return enrollments;
        }

        public async Task<(List<EnrollmentDto> enrollments, int total)> GetCourseEnrollmentsAsync(
            int courseId, int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation("Getting enrollments for course {CourseId}", courseId);

            var query = await _unitOfWork.Enrollments.GetCourseEnrollmentsAsync(courseId);
            var total = await query.CountAsync();

            var enrollmentEntities = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var enrollments = enrollmentEntities.Select(e => MapToEnrollmentDto(e)).ToList();
            return (enrollments, total);
        }

        public async Task<EnrollmentDto?> GetEnrollmentAsync(int enrollmentId)
        {
            _logger.LogInformation("Getting enrollment: {EnrollmentId}", enrollmentId);

            var enrollment = await _unitOfWork.Enrollments.GetEnrollmentWithDetailsAsync(enrollmentId);
            if (enrollment == null)
                return null;

            return MapToEnrollmentDto(enrollment);
        }

        public async Task<LessonProgressDto> UpdateLessonProgressAsync(
            int enrollmentId, int lessonId, int progressPercentage, bool isCompleted)
        {
            if (progressPercentage < 0 || progressPercentage > 100)
                throw new ArgumentException("Progress percentage must be between 0 and 100", nameof(progressPercentage));

            _logger.LogInformation("Updating lesson {LessonId} progress for enrollment {EnrollmentId}", 
                lessonId, enrollmentId);

            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId);
            if (enrollment == null)
                throw new InvalidOperationException("Enrollment not found");

            // Get or create lesson progress
            var progressRepo = _unitOfWork.GetRepository<LessonProgress>();
            var progress = await progressRepo.FirstOrDefaultAsync(p => 
                p.EnrollmentId == enrollmentId && p.LessonId == lessonId);

            if (progress == null)
            {
                progress = new LessonProgress
                {
                    EnrollmentId = enrollmentId,
                    LessonId = lessonId,
                    StudentUserId = enrollment.StudentUserId,
                    StartedAt = DateTime.UtcNow,
                    ProgressPercentage = progressPercentage,
                    IsCompleted = isCompleted,
                    CompletedAt = isCompleted ? DateTime.UtcNow : null
                };
                await progressRepo.AddAsync(progress);
            }
            else
            {
                progress.ProgressPercentage = progressPercentage;
                progress.IsCompleted = isCompleted;
                if (isCompleted && !progress.CompletedAt.HasValue)
                    progress.CompletedAt = DateTime.UtcNow;
                progress.UpdatedAt = DateTime.UtcNow;
                await progressRepo.UpdateAsync(progress);
            }

            return new LessonProgressDto
            {
                Id = progress.Id,
                LessonId = progress.LessonId,
                ProgressPercentage = progress.ProgressPercentage,
                IsCompleted = progress.IsCompleted,
                Score = progress.Score,
                CompletedAt = progress.CompletedAt,
                TimeSpentMinutes = progress.TimeSpentMinutes
            };
        }

        public async Task<bool> CompleteEnrollmentAsync(int enrollmentId, decimal? grade)
        {
            _logger.LogInformation("Completing enrollment: {EnrollmentId}", enrollmentId);

            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId);
            if (enrollment == null)
                return false;

            enrollment.Status = EnrollmentStatus.Completed;
            enrollment.CompletionPercentage = 100;
            enrollment.CompletedAt = DateTime.UtcNow;
            enrollment.Grade = grade;

            await _unitOfWork.Enrollments.UpdateAsync(enrollment);
            return true;
        }

        public async Task<bool> DropCourseAsync(int enrollmentId)
        {
            _logger.LogInformation("Dropping course for enrollment: {EnrollmentId}", enrollmentId);

            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId);
            if (enrollment == null)
                return false;

            enrollment.Status = EnrollmentStatus.Dropped;
            await _unitOfWork.Enrollments.UpdateAsync(enrollment);
            return true;
        }

        public async Task<bool> IsEnrolledAsync(string studentUserId, int courseId)
        {
            return await _unitOfWork.Enrollments.IsStudentEnrolledAsync(studentUserId, courseId);
        }

        private EnrollmentDto MapToEnrollmentDto(Enrollment enrollment)
        {
            return new EnrollmentDto
            {
                Id = enrollment.Id,
                StudentUserId = enrollment.StudentUserId,
                CourseId = enrollment.CourseId,
                CourseName = enrollment.Course?.Title,
                EnrolledAt = enrollment.EnrolledAt,
                CompletionPercentage = enrollment.CompletionPercentage,
                CompletedAt = enrollment.CompletedAt,
                Grade = enrollment.Grade,
                Status = enrollment.Status.ToString()
            };
        }
    }
}
