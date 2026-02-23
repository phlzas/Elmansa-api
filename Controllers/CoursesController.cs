using Elmansa_api.RequestResponse;
using Elmansa_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Elmansa_api.Controllers
{
    /// <summary>
    /// Course management endpoints
    /// Provides CRUD operations for courses
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(
            ICourseService courseService,
            ILogger<CoursesController> logger)
        {
            _courseService = courseService ?? throw new ArgumentNullException(nameof(courseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all courses (paginated)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<CourseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllCourses(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (courses, total) = await _courseService.GetAllCoursesAsync(pageNumber, pageSize);

                return Ok(new PaginatedResponse<CourseDto>
                {
                    Data = courses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all courses");
                return StatusCode(500, new { message = "An error occurred while fetching courses" });
            }
        }

        /// <summary>
        /// Get published courses only
        /// </summary>
        [HttpGet("published")]
        [ProducesResponseType(typeof(PaginatedResponse<CourseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedCourses(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (courses, total) = await _courseService.GetPublishedCoursesAsync(pageNumber, pageSize);

                return Ok(new PaginatedResponse<CourseDto>
                {
                    Data = courses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting published courses");
                return StatusCode(500, new { message = "An error occurred while fetching courses" });
            }
        }

        /// <summary>
        /// Get popular courses
        /// </summary>
        [HttpGet("popular")]
        [ProducesResponseType(typeof(List<CourseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPopularCourses([FromQuery] int take = 10)
        {
            try
            {
                var courses = await _courseService.GetPopularCoursesAsync(take);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular courses");
                return StatusCode(500, new { message = "An error occurred while fetching courses" });
            }
        }

        /// <summary>
        /// Search courses
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PaginatedResponse<CourseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchCourses(
            [FromQuery] string searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest(new { message = "Search term is required" });

                var (courses, total) = await _courseService.SearchCoursesAsync(searchTerm, pageNumber, pageSize);

                return Ok(new PaginatedResponse<CourseDto>
                {
                    Data = courses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching courses");
                return StatusCode(500, new { message = "An error occurred while searching courses" });
            }
        }

        /// <summary>
        /// Get courses by category
        /// </summary>
        [HttpGet("category/{category}")]
        [ProducesResponseType(typeof(PaginatedResponse<CourseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCoursesByCategory(
            string category,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                    return BadRequest(new { message = "Category is required" });

                var (courses, total) = await _courseService.GetCoursesByCategoryAsync(category, pageNumber, pageSize);

                return Ok(new PaginatedResponse<CourseDto>
                {
                    Data = courses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses by category");
                return StatusCode(500, new { message = "An error occurred while fetching courses" });
            }
        }

        /// <summary>
        /// Get course details by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCourseDetail(int id)
        {
            try
            {
                var course = await _courseService.GetCourseDetailAsync(id);
                if (course == null)
                    return NotFound(new { message = "Course not found" });

                return Ok(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course {CourseId}", id);
                return StatusCode(500, new { message = "An error occurred while fetching course details" });
            }
        }

        /// <summary>
        /// Create new course (requires authorization)
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var course = await _courseService.CreateCourseAsync(dto, userId);
                return CreatedAtAction(nameof(GetCourseDetail), new { id = course.Id }, course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course");
                return StatusCode(500, new { message = "An error occurred while creating the course" });
            }
        }

        /// <summary>
        /// Update course (requires authorization)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.Id = id;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var course = await _courseService.UpdateCourseAsync(dto, userId);
                if (course == null)
                    return NotFound(new { message = "Course not found" });

                return Ok(course);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized course update attempt");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the course" });
            }
        }

        /// <summary>
        /// Delete course (requires authorization)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var result = await _courseService.DeleteCourseAsync(id, userId);
                if (!result)
                    return NotFound(new { message = "Course not found" });

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized course deletion attempt");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the course" });
            }
        }

        /// <summary>
        /// Publish course (requires authorization)
        /// </summary>
        [HttpPost("{id}/publish")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PublishCourse(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var result = await _courseService.PublishCourseAsync(id, userId);
                if (!result)
                    return NotFound(new { message = "Course not found" });

                return Ok(new { message = "Course published successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing course {CourseId}", id);
                return StatusCode(500, new { message = "An error occurred while publishing the course" });
            }
        }

        /// <summary>
        /// Get my courses (requires authorization)
        /// </summary>
        [HttpGet("my-courses")]
        [Authorize]
        [ProducesResponseType(typeof(List<CourseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyCourses()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not found" });

                var courses = await _courseService.GetMyCoursesAsync(userId);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my courses");
                return StatusCode(500, new { message = "An error occurred while fetching your courses" });
            }
        }
    }
}
