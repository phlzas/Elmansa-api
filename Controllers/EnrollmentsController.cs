using Elmansa_api.RequestResponse;
using Elmansa_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Elmansa_api.Controllers
{
    /// <summary>
    /// Enrollment management endpoints
    /// Handles student course enrollment and progress tracking
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ILogger<EnrollmentsController> _logger;

        public EnrollmentsController(
            IEnrollmentService enrollmentService,
            ILogger<EnrollmentsController> logger)
        {
            _enrollmentService = enrollmentService ?? throw new ArgumentNullException(nameof(enrollmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get current user's enrollments
        /// </summary>
        [HttpGet("my-enrollments")]
        [ProducesResponseType(typeof(List<EnrollmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyEnrollments()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(userId);
                return Ok(enrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my enrollments");
                return StatusCode(500, new { message = "An error occurred while fetching enrollments" });
            }
        }

        /// <summary>
        /// Get course enrollments (admin only)
        /// </summary>
        [HttpGet("course/{courseId}")]
        [ProducesResponseType(typeof(PaginatedResponse<EnrollmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseEnrollments(
            int courseId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (enrollments, total) = await _enrollmentService.GetCourseEnrollmentsAsync(
                    courseId, pageNumber, pageSize);

                return Ok(new PaginatedResponse<EnrollmentDto>
                {
                    Data = enrollments,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course enrollments");
                return StatusCode(500, new { message = "An error occurred while fetching enrollments" });
            }
        }

        /// <summary>
        /// Get enrollment details
        /// </summary>
        [HttpGet("{enrollmentId}")]
        [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrollment(int enrollmentId)
        {
            try
            {
                var enrollment = await _enrollmentService.GetEnrollmentAsync(enrollmentId);
                if (enrollment == null)
                    return NotFound(new { message = "Enrollment not found" });

                return Ok(enrollment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment {EnrollmentId}", enrollmentId);
                return StatusCode(500, new { message = "An error occurred while fetching enrollment" });
            }
        }

        /// <summary>
        /// Enroll student in course
        /// </summary>
        [HttpPost("enroll")]
        [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnrollInCourse([FromBody] EnrollCourseRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var enrollment = await _enrollmentService.EnrollStudentAsync(userId, request.CourseId);
                return CreatedAtAction(nameof(GetEnrollment), new { enrollmentId = enrollment.Id }, enrollment);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid enrollment operation");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling in course");
                return StatusCode(500, new { message = "An error occurred while enrolling in course" });
            }
        }

        /// <summary>
        /// Update lesson progress
        /// </summary>
        [HttpPost("{enrollmentId}/lesson-progress")]
        [ProducesResponseType(typeof(LessonProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateLessonProgress(
            int enrollmentId,
            [FromBody] UpdateLessonProgressRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var progress = await _enrollmentService.UpdateLessonProgressAsync(
                    enrollmentId,
                    request.LessonId,
                    request.ProgressPercentage,
                    request.IsCompleted);

                return Ok(progress);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson progress");
                return StatusCode(500, new { message = "An error occurred while updating progress" });
            }
        }

        /// <summary>
        /// Complete enrollment
        /// </summary>
        [HttpPost("{enrollmentId}/complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteEnrollment(
            int enrollmentId,
            [FromBody] CompleteEnrollmentRequest? request = null)
        {
            try
            {
                var result = await _enrollmentService.CompleteEnrollmentAsync(
                    enrollmentId, request?.Grade);

                if (!result)
                    return NotFound(new { message = "Enrollment not found" });

                return Ok(new { message = "Enrollment completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing enrollment");
                return StatusCode(500, new { message = "An error occurred while completing enrollment" });
            }
        }

        /// <summary>
        /// Drop course
        /// </summary>
        [HttpPost("{enrollmentId}/drop")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DropCourse(int enrollmentId)
        {
            try
            {
                var result = await _enrollmentService.DropCourseAsync(enrollmentId);
                if (!result)
                    return NotFound(new { message = "Enrollment not found" });

                return Ok(new { message = "Course dropped successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dropping course");
                return StatusCode(500, new { message = "An error occurred while dropping course" });
            }
        }
    }

    /// <summary>
    /// Request model for enrolling in a course
    /// </summary>
    public class EnrollCourseRequest
    {
        public int CourseId { get; set; }
    }

    /// <summary>
    /// Request model for updating lesson progress
    /// </summary>
    public class UpdateLessonProgressRequest
    {
        public int LessonId { get; set; }
        public int ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// Request model for completing enrollment
    /// </summary>
    public class CompleteEnrollmentRequest
    {
        public decimal? Grade { get; set; }
    }
}
