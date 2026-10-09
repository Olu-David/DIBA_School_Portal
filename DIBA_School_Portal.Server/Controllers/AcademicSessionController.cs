using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Repository.IServices;
// using Microsoft.AspNetCore.Authorization;   // ← commented out for testing
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]   // ← disabled for testing
    public class AcademicSessionController : ControllerBase
    {
        private readonly IAcademicSession _service;

        public AcademicSessionController(IAcademicSession service)
        {
            _service = service;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "System";

        // CREATE
        [HttpPost("create")]
        // [Authorize(Roles = "SuperAdmin,Principal")]   // ← disabled for testing
        public async Task<IActionResult> CreateSession([FromBody] AcademicSessionDto dto)
        {
            dto.UserId = CurrentUserId;
            var result = await _service.CreateAcademicSessionAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        // READ ALL
        [HttpGet("all")]
        // [Authorize(Roles = "SuperAdmin,Principal,...")]   // ← disabled
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid sessionId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllSessionsAsync(sessionId, pageNumber, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        // READ BY ID
        [HttpGet("{id:guid}")]
        // [Authorize(Roles = "SuperAdmin,Principal,...")]   // ← disabled
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetSessionByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // SET CURRENT
        [HttpPost("set-current")]
        // [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal")]   // ← disabled
        public async Task<IActionResult> SetCurrent(
            [FromQuery] Guid sessionId,
            [FromQuery] Guid termId)
        {
            var success = await _service.SetCurrentSessionAsync(sessionId, termId);
            if (!success) return NotFound(new { message = "Session or term not found." });
            return Ok(new { message = "Current session set successfully." });
        }

        // UPDATE
        [HttpPut("update")]
        // [Authorize(Roles = "SuperAdmin,Principal")]   // ← disabled for testing
        public async Task<IActionResult> Update([FromBody] UpdateAcademicSessionRequest request)
        {
            request.Session.UserId = CurrentUserId;
            var result = await _service.UpdateAcademicSessionAsync(request);
            return StatusCode(result.StatusCode, result);
        }
    }
}