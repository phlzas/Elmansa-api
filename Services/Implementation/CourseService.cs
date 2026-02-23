using Elmansa_api.Models;
using Elmansa_api.Repositories;
using Elmansa_api.RequestResponse;
using Microsoft.EntityFrameworkCore;

namespace Elmansa_api.Services.Implementation
{
    /// <summary>
    /// Course service implementation
    /// Handles business logic for course operations
    /// </summary>
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CourseService> _logger;

        public CourseService(
            IUnitOfWork unitOfWork,
            ILogger<CourseService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(List<CourseDto> courses, int total)> GetAllCoursesAsync(
            int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation("Getting all courses - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            var query = await _unitOfWork.Courses.GetAllAsync();
            var total = await query.CountAsync();
            
            var courseEntities = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var courses = courseEntities.Select(c => MapToCourseDto(c)).ToList();
            return (courses, total);
        }

        public async Task<(List<CourseDto> courses, int total)> GetPublishedCoursesAsync(
            int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation("Getting published courses - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            var query = await _unitOfWork.Courses.GetPublishedCoursesAsync();
            var total = await query.CountAsync();

            var courseEntities = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var courses = courseEntities.Select(c => MapToCourseDto(c)).ToList();
            return (courses, total);
        }

        public async Task<(List<CourseDto> courses, int total)> GetCoursesByCategoryAsync(
            string category, int pageNumber = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Category cannot be empty", nameof(category));

            _logger.LogInformation("Getting courses by category: {Category}", category);

            var query = await _unitOfWork.Courses.GetCoursesByCategoryAsync(category);
            var total = await query.CountAsync();

            var courseEntities = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var courses = courseEntities.Select(c => MapToCourseDto(c)).ToList();
            return (courses, total);
        }

        public async Task<CourseDetailDto?> GetCourseDetailAsync(int courseId)
        {
            _logger.LogInformation("Getting course details for course: {CourseId}", courseId);

            var course = await _unitOfWork.Courses.GetCourseWithDetailsAsync(courseId);
            if (course == null)
            {
                _logger.LogWarning("Course {CourseId} not found", courseId);
                return null;
            }

            var courseDetail = MapToCourseDetailDto(course);
            return courseDetail;
        }

        public async Task<(List<CourseDto> courses, int total)> SearchCoursesAsync(
            string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

            _logger.LogInformation("Searching courses with term: {SearchTerm}", searchTerm);

            var query = await _unitOfWork.Courses.SearchCoursesAsync(searchTerm);
            var total = await query.CountAsync();

            var courseEntities = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var courses = courseEntities.Select(c => MapToCourseDto(c)).ToList();
            return (courses, total);
        }

        public async Task<List<CourseDto>> GetPopularCoursesAsync(int take = 10)
        {
            _logger.LogInformation("Getting {Count} popular courses", take);

            var query = await _unitOfWork.Courses.GetPopularCoursesAsync(take);
            var courseEntities = await query.ToListAsync();
            var courses = courseEntities.Select(c => MapToCourseDto(c)).ToList();

            return courses;
        }

        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, string createdByUserId)
        {
            if (string.IsNullOrWhiteSpace(createdByUserId))
                throw new ArgumentException("User ID cannot be empty", nameof(createdByUserId));

            _logger.LogInformation("Creating new course: {Title} by user {UserId}", dto.Title, createdByUserId);

            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Level = dto.Level,
                EstimatedHours = dto.EstimatedHours,
                ImageUrl = dto.ImageUrl,
                CreatedByUserId = createdByUserId,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCourse = await _unitOfWork.Courses.AddAsync(course);
            return MapToCourseDto(createdCourse);
        }

        public async Task<CourseDto?> UpdateCourseAsync(UpdateCourseDto dto, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            _logger.LogInformation("Updating course {CourseId}", dto.Id);

            var course = await _unitOfWork.Courses.GetByIdAsync(dto.Id);
            if (course == null)
            {
                _logger.LogWarning("Course {CourseId} not found", dto.Id);
                return null;
            }

            // Authorization check - only creator can update
            if (course.CreatedByUserId != userId)
            {
                _logger.LogWarning("User {UserId} not authorized to update course {CourseId}", userId, dto.Id);
                throw new UnauthorizedAccessException("You are not authorized to update this course");
            }

            // Update properties
            if (!string.IsNullOrWhiteSpace(dto.Title))
                course.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.Description))
                course.Description = dto.Description;
            if (!string.IsNullOrWhiteSpace(dto.Category))
                course.Category = dto.Category;
            if (!string.IsNullOrWhiteSpace(dto.Level))
                course.Level = dto.Level;
            if (dto.EstimatedHours.HasValue)
                course.EstimatedHours = dto.EstimatedHours.Value;
            if (dto.ImageUrl != null)
                course.ImageUrl = dto.ImageUrl;
            if (dto.IsPublished.HasValue)
                course.IsPublished = dto.IsPublished.Value;

            course.UpdatedAt = DateTime.UtcNow;

            var updatedCourse = await _unitOfWork.Courses.UpdateAsync(course);
            return MapToCourseDto(updatedCourse);
        }

        public async Task<bool> DeleteCourseAsync(int courseId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            _logger.LogInformation("Deleting course {CourseId}", courseId);

            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null)
                return false;

            // Authorization check
            if (course.CreatedByUserId != userId)
                throw new UnauthorizedAccessException("You are not authorized to delete this course");

            return await _unitOfWork.Courses.DeleteAsync(courseId);
        }

        public async Task<bool> PublishCourseAsync(int courseId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            _logger.LogInformation("Publishing course {CourseId}", courseId);

            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null)
                return false;

            // Authorization check
            if (course.CreatedByUserId != userId)
                throw new UnauthorizedAccessException("You are not authorized to publish this course");

            course.IsPublished = true;
            course.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Courses.UpdateAsync(course);
            return true;
        }

        public async Task<List<CourseDto>> GetMyCoursesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            _logger.LogInformation("Getting courses created by user: {UserId}", userId);

            var query = await _unitOfWork.Courses.GetCoursesByCreatorAsync(userId);
            var courseEntities = await query.ToListAsync();
            var courses = courseEntities.Select(c => MapToCourseDto(c)).ToList();

            return courses;
        }

        // Helper methods
        private CourseDto MapToCourseDto(Course course)
        {
            return new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Category = course.Category,
                Level = course.Level,
                LessonCount = course.LessonCount,
                EstimatedHours = course.EstimatedHours,
                ImageUrl = course.ImageUrl,
                IsPublished = course.IsPublished,
                CreatedByUserId = course.CreatedByUserId,
                CreatedByUserName = course.CreatedByUser?.UserName,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                EnrollmentCount = course.Enrollments?.Count ?? 0
            };
        }

        private CourseDetailDto MapToCourseDetailDto(Course course)
        {
            return new CourseDetailDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Category = course.Category,
                Level = course.Level,
                LessonCount = course.LessonCount,
                EstimatedHours = course.EstimatedHours,
                ImageUrl = course.ImageUrl,
                IsPublished = course.IsPublished,
                CreatedByUserId = course.CreatedByUserId,
                CreatedByUserName = course.CreatedByUser?.UserName,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                EnrollmentCount = course.Enrollments?.Count ?? 0,
                Lessons = course.Lessons
                    .OrderBy(l => l.Order)
                    .Select(l => new LessonDto
                    {
                        Id = l.Id,
                        Title = l.Title,
                        Content = l.Content,
                        Order = l.Order,
                        DurationMinutes = l.DurationMinutes,
                        IsOptional = l.IsOptional,
                        VideoUrl = l.VideoUrl,
                        ResourcesUrl = l.ResourcesUrl,
                        CreatedAt = l.CreatedAt
                    })
                    .ToList()
            };
        }
    }
}
