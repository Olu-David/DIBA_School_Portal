using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ClassDto;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SchoolClassController : ControllerBase
    {
        private readonly ISchoolClassService _service;

        public SchoolClassController(ISchoolClassService service)
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
        public async Task<IActionResult> Create([FromBody] SchoolClassDTO dto)
        {
            var result = await _service.CreateSchoolClassAsync(dto, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // UPDATE — SuperAdmin or Principal
        [HttpPut("update")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> Update([FromBody] SchoolClassDTO dto)
        {
            var result = await _service.UpdateSchoolClassAsync(dto, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // SOFT DELETE — SuperAdmin or Principal
        [HttpDelete("soft-delete/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var ok = await _service.SoftDeleteSchoolClass(id, CurrentUserId);
            return ok ? Ok(new { message = "Class soft-deleted." }) : NotFound();
        }

        // RESTORE — SuperAdmin or Principal
        [HttpPost("restore/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> Restore(Guid id)
        {
            var ok = await _service.RestoreSchoolClass(id, CurrentUserId);
            return ok ? Ok(new { message = "Class restored." }) : NotFound();
        }

        // UPDATE SUBJECTS — SuperAdmin or Principal
      

        [HttpPut("subjects/{classId:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> UpdateSubjects(
    Guid classId, [FromBody] List<ClassSubjectAssignmentDto> items)
        {
            var result = await _service.UpdateClassSubjectsAsync(classId, items, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // READ ALL — all staff incl. Cashier
        [HttpGet("all")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Cashier,Teacher,FormTeacher")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllSchoolClassesAsync();
            return Ok(result);
        }

        // READ BY ID — all staff incl. Cashier
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Cashier,Teacher,FormTeacher")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetSchoolClassByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // GET SUBJECTS FOR CLASS — all staff (no Cashier, not needed)
        [HttpGet("{classId:guid}/subjects")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Teacher,FormTeacher")]
        public async Task<IActionResult> GetSubjects(Guid classId)
        {
            var result = await _service.GetSubjectsForClassAsync(classId);
            return Ok(result);
        }

        // GET CHECKBOXES — admin roles only
        [HttpGet("{classId:guid}/subjects-selection")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,SchoolAdmin")]
        public async Task<IActionResult> GetSubjectsSelection(Guid classId)
        {
            var result = await _service.GetClassSubjectsWithSelectionAsync(classId);
            return Ok(result);
        }
    }
}