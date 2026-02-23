namespace Elmansa_api.RequestResponse
{
    /// <summary>
    /// Generic paginated response wrapper
    /// </summary>
    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }
    /// <summary>
    /// DTO for creating a course
    /// </summary>
    public class CreateCourseDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Level { get; set; } = "Beginner";
        public decimal EstimatedHours { get; set; }
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// DTO for updating a course
    /// </summary>
    public class UpdateCourseDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Level { get; set; }
        public decimal? EstimatedHours { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsPublished { get; set; }
    }

    /// <summary>
    /// DTO for course response
    /// </summary>
    public class CourseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Level { get; set; } = null!;
        public int LessonCount { get; set; }
        public decimal EstimatedHours { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPublished { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int EnrollmentCount { get; set; }
    }

    /// <summary>
    /// DTO for detailed course with lessons
    /// </summary>
    public class CourseDetailDto : CourseDto
    {
        public List<LessonDto> Lessons { get; set; } = new();
    }

    /// <summary>
    /// DTO for lesson
    /// </summary>
    public class LessonDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int Order { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsOptional { get; set; }
        public string? VideoUrl { get; set; }
        public string? ResourcesUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for enrollment
    /// </summary>
    public class EnrollmentDto
    {
        public int Id { get; set; }
        public string StudentUserId { get; set; } = null!;
        public int CourseId { get; set; }
        public string? CourseName { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int CompletionPercentage { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal? Grade { get; set; }
        public string Status { get; set; } = null!;
    }

    /// <summary>
    /// DTO for lesson progress
    /// </summary>
    public class LessonProgressDto
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string? LessonTitle { get; set; }
        public int ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public decimal? Score { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? TimeSpentMinutes { get; set; }
    }
}
