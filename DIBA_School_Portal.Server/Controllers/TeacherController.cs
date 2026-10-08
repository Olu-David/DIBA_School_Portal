using DIBA_School_Portal.Server.Contracts.Teacher;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeacherController : ControllerBase
    {
        #region Fields
        private readonly ITeacherService _teacherService;

        public TeacherController(ITeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        private string Ip =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        #endregion

        // ==========================================================
        // LIST — paginated + filterable
        // ==========================================================
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant")]
        public async Task<IActionResult> GetTeachers([FromQuery] TeacherFilterDto filter)
        {
            var result = await _teacherService.GetTeachersAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // GET BY ID — full detail with subjects + classes
        // ==========================================================
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Teacher,FormTeacher")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _teacherService.GetTeacherByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // GET BY STAFF ID
        // ==========================================================
        [HttpGet("by-staff-id/{staffId}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Teacher,FormTeacher")]
        public async Task<IActionResult> GetByStaffId(string staffId)
        {
            var result = await _teacherService.GetTeacherByStaffIdAsync(staffId);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // GET BY USER ID
        // ==========================================================
        [HttpGet("by-user/{userId}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Teacher,FormTeacher")]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            var result = await _teacherService.GetTeacherByUserIdAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // MY OWN PROFILE (self-service)
        // ==========================================================
        [HttpGet("me")]
        [Authorize(Roles = "Teacher,FormTeacher")]
        public async Task<IActionResult> GetMyProfile()
        {
            var result = await _teacherService.GetTeacherByUserIdAsync(CurrentUserId);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // UPDATE
        // ==========================================================
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,SchoolAdmin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeacherDto request)
        {
            var result = await _teacherService.UpdateTeacherAsync(
                id, request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // SOFT DELETE
        // ==========================================================
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,SchoolAdmin")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await _teacherService.SoftDeleteTeacherAsync(id, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // RESTORE
        // ==========================================================
        [HttpPost("{id:guid}/restore")]
        [Authorize(Roles = "SuperAdmin,Principal,SchoolAdmin")]
        public async Task<IActionResult> Restore(Guid id)
        {
            var result = await _teacherService.RestoreTeacherAsync(id, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // ASSIGN SUBJECTS — full replace
        // ==========================================================
        [HttpPut("{teacherId:guid}/subjects")]
        [Authorize(Roles = "SuperAdmin,Principal,SchoolAdmin")]
        public async Task<IActionResult> AssignSubjects(
            Guid teacherId, [FromBody] List<Guid> subjectIds)
        {
            var result = await _teacherService.AssignSubjectsAsync(
                teacherId, subjectIds, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // UNASSIGN ONE SUBJECT
        // ==========================================================
        [HttpDelete("{teacherId:guid}/subjects/{subjectId:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,SchoolAdmin")]
        public async Task<IActionResult> UnassignSubject(Guid teacherId, Guid subjectId)
        {
            var result = await _teacherService.UnassignSubjectAsync(
                teacherId, subjectId, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // ASSIGN CLASSES — full replace
        // ==========================================================
        [HttpPut("{teacherId:guid}/classes")]
        [Authorize(Roles = "SuperAdmin,Principal,SchoolAdmin")]
        public async Task<IActionResult> AssignClasses(
            Guid teacherId, [FromBody] List<Guid> classIds)
        {
            var result = await _teacherService.AssignClassesAsync(
                teacherId, classIds, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // UNASSIGN ONE CLASS
        // ==========================================================
        [HttpDelete("{teacherId:guid}/classes/{classId:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,SchoolAdmin")]
        public async Task<IActionResult> UnassignClass(Guid teacherId, Guid classId)
        {
            var result = await _teacherService.UnassignClassAsync(
                teacherId, classId, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }
    }
}