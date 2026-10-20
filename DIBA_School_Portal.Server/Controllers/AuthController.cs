using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{ 
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        #region Fields
        private readonly IAuthService _auth;
        private UserManager<ApplicationUser> _userManager;
        private AppDbContext _context;

        public AuthController(IAuthService auth, UserManager<ApplicationUser> userManager, AppDbContext Context)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _userManager = userManager;
            _context = Context;
        }

        // Current user's ID from JWT
        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("Missing user ID claim.");

        // Client IP for audit trail
        private string Ip =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        #endregion

        #region Login - Student
        [HttpPost("student/login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> StudentLogin([FromBody] StudentLoginRequestDto request)
        {
            var result = await _auth.StudentLoginAsync(request, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Login - Staff
        [HttpPost("staff/login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> StaffLogin([FromBody] StaffLoginRequestDto request)
        {
            var result = await _auth.StaffLoginAsync(request, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Login - Parent
        [HttpPost("parent/login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ParentLogin([FromBody] ParentLoginRequestDto request)
        {
            var result = await _auth.ParentLoginAsync(request, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Create Account
        [HttpPost("create-account")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ServiceResponse<CreateAccountResponseDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateAccount([FromForm] CreateAccountDto request)
        {
            var result = await _auth.CreateAccountAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Link Parent To Student
        [HttpPost("link-parent")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        [ProducesResponseType(typeof(ServiceResponse<CreateAccountResponseDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> LinkParent([FromBody] LinkParentToStudentDto request)
        {
            var result = await _auth.LinkParentToStudentAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Change Password
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var result = await _auth.ChangePasswordAsync(CurrentUserId, request, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Admin Reset Password
        [HttpPost("admin-reset-password")]
        [Authorize(Roles = "SuperAdmin,Principal,SchoolAdmin,Registrar")]   // ← add SchoolAdmin
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdminResetPassword([FromBody] AdminResetPasswordDto request)
        {
            var result = await _auth.AdminResetPasswordAsync(CurrentUserId, request, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Update Profile
        [HttpPut("profile")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            var result = await _auth.UpdateProfileAsync(CurrentUserId, request, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Update Profile Picture
        [HttpPost("profile-picture")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile picture)
        {
            var result = await _auth.UpdateProfilePictureAsync(CurrentUserId, picture, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Change User Role
        /// <summary>
        /// Changes a staff member's role (e.g. Teacher → FormTeacher, Accountant, etc.).
        /// SuperAdmin and Principal only. Principal cannot touch leadership accounts
        /// or assign leadership roles.
        /// </summary>
        [HttpPut("users/{userId}/role")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeRole(
            string userId,
            [FromBody] ChangeRoleDto request)
        {
            var result = await _auth.ChangeRoleAsync(userId, request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.ProfilePictureUrl,
                Role = user.PrimaryRole,
                user.ClassId,
                ClassName = user.SchoolClass?.Name??"",
                user.MustChangePassword,
                user.UpdatedAt,
                IsGraduated = student?.IsGraduated ?? false,
                GraduationDate = student?.GraduationDate,
                GraduationSession = student?.GraduationSession,
            });
        }
    }
}
