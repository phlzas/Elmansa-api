namespace Elmansa_api.Models
{
    /// <summary>
    /// Represents an educational course/class on the platform
    /// </summary>
    public class Course
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Course title
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// Detailed course description
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// Course category (e.g., "Mathematics", "Science", "Languages")
        /// </summary>
        public string Category { get; set; } = null!;

        /// <summary>
        /// Difficulty level (Beginner, Intermediate, Advanced)
        /// </summary>
        public string Level { get; set; } = "Beginner";

        /// <summary>
        /// Number of lessons in this course
        /// </summary>
        public int LessonCount { get; set; } = 0;

        /// <summary>
        /// Estimated hours to complete the course
        /// </summary>
        public decimal EstimatedHours { get; set; }

        /// <summary>
        /// Course thumbnail/cover image URL
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Whether course is published and visible
        /// </summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// User who created/teaches the course
        /// </summary>
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        /// <summary>
        /// When the course was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the course was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation: Lessons in this course
        /// </summary>
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

        /// <summary>
        /// Navigation: Student enrollments
        /// </summary>
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
