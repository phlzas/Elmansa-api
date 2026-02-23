namespace Elmansa_api.Models
{
    /// <summary>
    /// Represents a lesson within a course
    /// </summary>
    public class Lesson
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Lesson title
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// Detailed lesson content
        /// </summary>
        public string Content { get; set; } = null!;

        /// <summary>
        /// Lesson order within the course
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Estimated time to complete in minutes
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Whether this lesson is marked as completed by AI (optional content)
        /// </summary>
        public bool IsOptional { get; set; } = false;

        /// <summary>
        /// Video URL if lesson includes video content
        /// </summary>
        public string? VideoUrl { get; set; }

        /// <summary>
        /// Resources or attachments for this lesson
        /// </summary>
        public string? ResourcesUrl { get; set; }

        /// <summary>
        /// Foreign key to parent course
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Navigation: Parent course
        /// </summary>
        public Course Course { get; set; } = null!;

        /// <summary>
        /// When the lesson was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the lesson was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation: Lesson progress per user
        /// </summary>
        public ICollection<LessonProgress> Progress { get; set; } = new List<LessonProgress>();
    }
}
