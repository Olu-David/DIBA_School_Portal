using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly AppDbContext _context;

        public StudentController(IStudentService studentService, AppDbContext Context)
        {
            _studentService = studentService;
            _context = Context;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        private string Ip =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // ==========================================================
        // LIST (paginated + filterable) — Cashier YES
        // ==========================================================
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Cashier,Teacher,FormTeacher")]
        public async Task<IActionResult> GetStudents([FromQuery] StudentFilterDto filter)
        {
            var result = await _studentService.GetStudentsAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // GET BY ID — Cashier YES
        // ==========================================================
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Cashier,Teacher,FormTeacher")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _studentService.GetStudentByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // GET BY ADMISSION NUMBER — Cashier YES
        // ==========================================================
        [HttpGet("by-admission/{admissionNumber}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Cashier,Teacher,FormTeacher")]
        public async Task<IActionResult> GetByAdmission(string admissionNumber)
        {
            var result = await _studentService.GetStudentByAdmissionNumberAsync(admissionNumber);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // GET BY CLASS — Cashier YES
        // ==========================================================
        [HttpGet("by-class/{classId:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant,Cashier,Teacher,FormTeacher")]
        public async Task<IActionResult> GetByClass(Guid classId, [FromQuery] bool onlyActive = true)
        {
            var result = await _studentService.GetStudentsByClassAsync(classId, onlyActive);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // UPDATE — admin only (no Cashier)
        // ==========================================================
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar,SchoolAdmin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentDto request)
        {
            var result = await _studentService.UpdateStudentAsync(
                id, request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // SOFT DELETE — admin only
        // ==========================================================
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await _studentService.SoftDeleteStudentAsync(id, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // RESTORE — admin only
        // ==========================================================
        [HttpPost("{id:guid}/restore")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        public async Task<IActionResult> Restore(Guid id)
        {
            var result = await _studentService.RestoreStudentAsync(id, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // MY BILL — Student-only
        // ==========================================================
        [HttpGet("me/bill")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyBill(
            [FromQuery] Guid? termId = null,
            [FromQuery] Guid? academicSessionId = null)
        {
            var result = await _studentService.GetMyBillAsync(
                CurrentUserId, termId, academicSessionId);
            return StatusCode(result.StatusCode, result);
        }
        // StudentController.cs
        [HttpGet("me/subjects")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMySubjects()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new ServiceResponse<StudentSubjectDto>
                {
                    Success = false,
                    Message = "Student profile not found."
                });

            var subjects = await _context.StudentSubjects
                .Where(ss => ss.StudentId == student.Id )
                .Include(ss => ss.Subject)
                    .ThenInclude(s => s!.TeacherSubjects)
                        .ThenInclude(ts => ts.Teacher)
                .Select(ss => new StudentSubjectDto
                {
                    subjectId = ss.Subject!.Id,
                    subjectName = ss.Subject.Name,
                    subjectCode = ss.Subject.Code,
                    teacherName = ss.Subject.TeacherSubjects
                        .Where(ts => ts.Teacher != null)
                        .Select(ts => ts.Teacher.FirstName + " " + ts.Teacher.LastName)
                        .FirstOrDefault() ?? "—",
                  
                })
                .ToListAsync();
            return Ok(new ServiceResponse<List<StudentSubjectDto>>
            {
                Success = true,
                StatusCode = 200,
                Data = subjects
            });


        }
        [HttpGet("me/subjects/count")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMySubjectsCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new ServiceResponse<int>
                {
                    Success = false,
                    StatusCode = 404,
                    Message = "Student profile not found."
                });

            var count = await _context.StudentSubjects
                .Where(ss => ss.StudentId == student.Id)
                .CountAsync();

            return Ok(new ServiceResponse<int>
            {
                Success = true,
                StatusCode = 200,
                Data = count
            });
        }
    }
}