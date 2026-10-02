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
    public class SchoolFeeController : ControllerBase
    {
        private readonly ISchoolFeeService _service;

        public SchoolFeeController(ISchoolFeeService service)
        {
            _service = service;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "System";

        // FEE CREATION METADATA — SuperAdmin, Principal, Accountant
        [HttpGet("metadata")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant")]
        public async Task<IActionResult> GetMetadata()
        {
            var result = await _service.GetFeeCreationMetadataAsync();
            return StatusCode(result.StatusCode, result);
        }

        // CREATE FEE — SuperAdmin, Principal, Accountant
        [HttpPost("create-fee")]
        [Authorize(Roles = "SuperAdmin,Principal,Accountant")]
        public async Task<IActionResult> CreateFee([FromBody] FeeDto dto)
        {
            var result = await _service.CreateFeeAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        // GET CLASSES FOR SUBJECT — all staff
        [HttpGet("classes-for-subject/{subjectId:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Teacher,FormTeacher")]
        public async Task<IActionResult> GetClassesForSubject(Guid subjectId)
        {
            var result = await _service.GetClassesForSubjectAsync(subjectId);
            return StatusCode(result.StatusCode, result);
        }

        // GET CLASS BY ID — all staff
        [HttpGet("class/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Teacher,FormTeacher")]
        public async Task<IActionResult> GetClassById(Guid id)
        {
            var result = await _service.GetSchoolClassByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        // UPDATE CLASS — SuperAdmin, Principal
        [HttpPut("class/update")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> UpdateClass([FromBody] SchoolClassDTO dto)
        {
            var result = await _service.UpdateSchoolClassAsync(dto, CurrentUserId);
            return StatusCode(result.StatusCode, result);
        }

        // SOFT DELETE CLASS — SuperAdmin, Principal
        [HttpDelete("class/soft-delete/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> SoftDeleteClass(Guid id)
        {
            var ok = await _service.SoftDeleteSchoolClass(id, CurrentUserId);
            return ok ? Ok() : NotFound();
        }

        // RESTORE CLASS — SuperAdmin, Principal
        [HttpPost("class/restore/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> RestoreClass(Guid id)
        {
            var ok = await _service.RestoreSchoolClass(id, CurrentUserId);
            return ok ? Ok() : NotFound();
        }

        // GET SUBJECTS FOR CLASS — all staff
        [HttpGet("class/{classId:guid}/subjects")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Teacher,FormTeacher")]
        public async Task<IActionResult> GetSubjects(Guid classId)
        {
            var result = await _service.GetSubjectsForClassAsync(classId);
            return StatusCode(result.StatusCode, result);
        }

        // CHECKBOXES — admin only
        [HttpGet("class/{classId:guid}/subjects-selection")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,SchoolAdmin")]
        public async Task<IActionResult> GetSelection(Guid classId)
        {
            var result = await _service.GetClassSubjectsWithSelectionAsync(classId);
            return StatusCode(result.StatusCode, result);
        }

        // UPDATE SUBJECTS — SuperAdmin, Principal
        [HttpPut("class/{classId:guid}/update-subjects")]
        [Authorize(Roles = "SuperAdmin,Principal")]
        public async Task<IActionResult> UpdateSubjects(Guid classId, [FromBody] List<Guid> subjectIds)
        {
            var result = await _service.UpdateClassSubjectsAsync(classId, subjectIds, CurrentUserId);
            return StatusCode(result.StatusCode, result);
        }
    }
}