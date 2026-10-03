using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        #region Fields
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
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
        /// <summary>
        /// Student login with admission number (e.g. "DIBA/2026/001") and password.
        /// </summary>
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
        /// <summary>
        /// Staff login with school email and password.
        /// </summary>
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
        /// <summary>
        /// Parent login with phone number (preferred) or email and password.
        /// </summary>
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
        /// <summary>
        /// Creates a single account (staff, student, or parent).
        /// SuperAdmin, Principal, Registrar only.
        /// </summary>
        [HttpPost("create-account")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        [ProducesResponseType(typeof(ServiceResponse<CreateAccountResponseDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto request)
        {
            var result = await _auth.CreateAccountAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion

        #region Link Parent To Student
        /// <summary>
        /// Links a parent to a student. Creates the parent account if new.
        /// SuperAdmin, Principal, Registrar only.
        /// </summary>
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
        /// <summary>
        /// Self-service password change for any authenticated user.
        /// </summary>
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
        /// <summary>
        /// Admin-initiated password reset (no email required).
        /// SuperAdmin, Principal, Registrar only.
        /// </summary>
        [HttpPost("admin-reset-password")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdminResetPassword([FromBody] AdminResetPasswordDto request)
        {
            var result = await _auth.AdminResetPasswordAsync(CurrentUserId, request, Ip);
            return StatusCode(result.StatusCode, result);
        }
        #endregion
    }
}