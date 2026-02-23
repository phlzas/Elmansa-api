namespace Elmansa_api.Models
{
    /// <summary>
    /// Represents a student's enrollment in a course
    /// </summary>
    public class Enrollment
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
        /// Course ID
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Navigation: Enrolled course
        /// </summary>
        public Course Course { get; set; } = null!;

        /// <summary>
        /// When the student enrolled
        /// </summary>
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Completion percentage (0-100)
        /// </summary>
        public int CompletionPercentage { get; set; } = 0;

        /// <summary>
        /// When the course was completed (null if not completed)
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Student's overall grade/score for the course (0-100)
        /// </summary>
        public decimal? Grade { get; set; }

        /// <summary>
        /// Status of enrollment (Active, Completed, Dropped, Suspended)
        /// </summary>
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

        /// <summary>
        /// Navigation: Lesson progress entries
        /// </summary>
        public ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
    }

    /// <summary>
    /// Enrollment status enum
    /// </summary>
    public enum EnrollmentStatus
    {
        Active = 0,
        Completed = 1,
        Dropped = 2,
        Suspended = 3
    }
}
