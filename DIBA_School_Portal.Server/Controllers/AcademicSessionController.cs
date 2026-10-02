using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        // CREATE — SuperAdmin or Principal only
        [HttpPost("create")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> CreateSession([FromBody] AcademicSessionDto dto)
        {
            dto.UserId = CurrentUserId;
            var result = await _service.CreateAcademicSessionAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        // READ ALL — every admin-level role
        [HttpGet("all")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Accountant,Registrar,Teacher,FormTeacher")]
        public async Task<IActionResult> GetAll([FromQuery] Guid sessionId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllSessionsAsync(sessionId, pageNumber, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        // READ BY ID — every admin-level role
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Accountant,Registrar,Teacher,FormTeacher")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetSessionByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // SET CURRENT — SuperAdmin, Principal, VP
        [HttpPost("set-current")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal")]
        public async Task<IActionResult> SetCurrent([FromQuery] Guid sessionId, [FromQuery] Guid termId)
        {
            var success = await _service.SetCurrentSessionAsync(sessionId, termId);
            if (!success) return NotFound(new { message = "Session or term not found." });
            return Ok(new { message = "Current session set successfully." });
        }

        // UPDATE — SuperAdmin, Principal
        [HttpPut("update")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> Update([FromBody] AcademicSessionDto request, SchoolTermDto term)
        {
            request.UserId = CurrentUserId;
            var result = await _service.UpdateAcademicSessionAsync(request, term);
            return StatusCode(result.StatusCode, result);
        }
    }

 
}