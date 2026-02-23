namespace Elmansa_api.Models
{
    /// <summary>
    /// Tracks a student's progress through individual lessons
    /// </summary>
    public class LessonProgress
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Student user ID
        /// </summary>
        public string StudentUserId { get; set; } = null!;

        /// <summary>
        /// Navigation: Student user
        /// </summary>
        public ApplicationUser StudentUser { get; set; } = null!;

        /// <summary>
        /// Lesson ID
        /// </summary>
        public int LessonId { get; set; }

        /// <summary>
        /// Navigation: Lesson
        /// </summary>
        public Lesson Lesson { get; set; } = null!;

        /// <summary>
        /// Enrollment record
        /// </summary>
        public int EnrollmentId { get; set; }

        /// <summary>
        /// Navigation: Enrollment
        /// </summary>
        public Enrollment Enrollment { get; set; } = null!;

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; } = 0;

        /// <summary>
        /// Whether the lesson has been completed
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Score for this lesson (0-100)
        /// </summary>
        public decimal? Score { get; set; }

        /// <summary>
        /// When the student first started this lesson
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When the lesson was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Time spent on lesson in minutes
        /// </summary>
        public int? TimeSpentMinutes { get; set; }

        /// <summary>
        /// Student's notes or comments about the lesson
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// When this progress record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this progress record was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
