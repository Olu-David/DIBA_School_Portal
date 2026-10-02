using DIBA_School_Portal.Server.Contracts;
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
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        private string Ip =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        // =====================================================
        // LOGIN ENDPOINTS (anonymous)
        // =====================================================

        [HttpPost("student/login")]
        [AllowAnonymous]
        public async Task<IActionResult> StudentLogin([FromBody] StudentLoginRequestDto request)
        {
            var result = await _auth.StudentLoginAsync(request, Ip);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("staff/login")]
        [AllowAnonymous]
        public async Task<IActionResult> StaffLogin([FromBody] StaffLoginRequestDto request)
        {
            var result = await _auth.StaffLoginAsync(request, Ip);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("parent/login")]
        [AllowAnonymous]
        public async Task<IActionResult> ParentLogin([FromBody] ParentLoginRequestDto request)
        {
            var result = await _auth.ParentLoginAsync(request, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // =====================================================
        // ACCOUNT CREATION (restricted)
        // =====================================================

        [HttpPost("create-account")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto request)
        {
            var result = await _auth.CreateAccountAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("admit-student")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        public async Task<IActionResult> AdmitStudent([FromBody] CreateStudentWithParentDto request)
        {
            var result = await _auth.CreateStudentWithParentAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("link-parent")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        public async Task<IActionResult> LinkParent([FromBody] LinkParentToStudentDto request)
        {
            var result = await _auth.LinkParentToStudentAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // =====================================================
        // PASSWORD
        // =====================================================

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var result = await _auth.ChangePasswordAsync(CurrentUserId, request, Ip);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("admin-reset-password")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        public async Task<IActionResult> AdminReset([FromBody] AdminResetPasswordDto request)
        {
            var result = await _auth.AdminResetPasswordAsync(CurrentUserId, request, Ip);
            return StatusCode(result.StatusCode, result);
        }
    }
}