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
    public class SubjectController : ControllerBase
    {
        private readonly ISubjectService _service;

        public SubjectController(ISubjectService service)
        {
            _service = service;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "System";

        // CREATE — SuperAdmin or Principal
        [HttpPost("create")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> Create([FromBody] SubjectDto dto)
        {
            var result = await _service.CreateSubjectAsync(dto, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // UPDATE — SuperAdmin or Principal
        [HttpPut("update")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> Update([FromBody] SubjectDto dto)
        {
            var result = await _service.UpdateSubjectAsync(dto, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // SOFT DELETE — SuperAdmin or Principal
        [HttpDelete("soft-delete/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var ok = await _service.SoftDeleteSubject(id, CurrentUserId);
            return ok ? Ok() : NotFound();
        }

        // RESTORE — SuperAdmin or Principal
        [HttpPost("restore/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> Restore(Guid id)
        {
            var ok = await _service.RestoreSubject(id, CurrentUserId);
            return ok ? Ok() : NotFound();
        }

        // READ — all staff + students
        [HttpGet("all")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Teacher,FormTeacher,Student,Parent")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllSubjectsAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Teacher,FormTeacher,Student,Parent")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetSubjectByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}