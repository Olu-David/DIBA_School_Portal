using DIBA_School_Portal.Server.Contracts.ClassDto;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TermSubjectResultController : ControllerBase
    {
        private readonly ITermSubjectResultService _service;

        public TermSubjectResultController(ITermSubjectResultService service)
        {
            _service = service;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "System";

        // ==========================================================
        // MY ASSIGNMENTS — teacher's own classes + subjects
        // ==========================================================
        [HttpGet("my-assignments")]
        [Authorize(Roles = "Teacher,FormTeacher")]
        public async Task<IActionResult> GetMyAssignments(
            [FromQuery] Guid? academicSessionId = null,
            [FromQuery] Guid? termId = null)
        {
            var r = await _service.GetMyAssignmentsAsync(CurrentUserId, academicSessionId, termId);
            return StatusCode(r.StatusCode, r);
        }

        // ==========================================================
        // SCORE SHEET — the list of students for one class + subject
        // ==========================================================
        [HttpGet("score-sheet")]
        [Authorize(Roles = "Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin")]
        public async Task<IActionResult> GetScoreSheet(
            [FromQuery] Guid classId,
            [FromQuery] Guid subjectId,
            [FromQuery] Guid termId,
            [FromQuery] Guid academicSessionId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var r = await _service.GetStudentsForScoreEntryAsync(
                classId, subjectId, termId, academicSessionId,
                CurrentUserId, pageNumber, pageSize);
            return StatusCode(r.StatusCode, r);
        }

        // ==========================================================
        // SAVE SCORES — draft or final submit
        // ==========================================================
        [HttpPost("save-scores")]
        [Authorize(Roles = "Teacher,FormTeacher")]
        public async Task<IActionResult> SaveScores([FromBody] ClassScoreSubmissionDto model)
        {
            var r = await _service.SaveAndComputeSubjectScoresAsync(model, CurrentUserId);
            return StatusCode(r.StatusCode, r);
        }

        // ==========================================================
        // UPDATE SINGLE RESULT — teacher edit / admin override
        // ==========================================================
        [HttpPut("update-result/{subjectResultId:guid}")]
        [Authorize(Roles = "Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin")]
        public async Task<IActionResult> UpdateResult(
            Guid subjectResultId,
            [FromQuery] decimal test1,
            [FromQuery] decimal test2,
            [FromQuery] decimal exam)
        {
            var r = await _service.UpdateTermSubjectResultAsync(
                subjectResultId, test1, test2, exam, CurrentUserId);
            return StatusCode(r.StatusCode, r);
        }

        // ==========================================================
        // REPORT CARD — for one student, one term
        // ==========================================================
        [HttpGet("report-card/{studentId}")]
        [Authorize(Roles = "Student,Parent,Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin")]
        public async Task<IActionResult> GetReportCard(
            string studentId,
            [FromQuery] Guid termId,
            [FromQuery] Guid academicSessionId)
        {
            var r = await _service.GetStudentReportCardAsync(
                studentId, termId, academicSessionId);
            return StatusCode(r.StatusCode, r);
        }

        // ==========================================================
        // SCHOOL-WIDE RESULTS — live view for admins
        // ==========================================================
        [HttpGet("school")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Headmaster,Headmistress,SchoolAdmin")]
        public async Task<IActionResult> GetSchoolResults(
            [FromQuery] Guid academicSessionId,
            [FromQuery] Guid termId,
            [FromQuery] AcademicSection? section = null)
        {
            var r = await _service.GetAllSchoolResultsAsync(
                academicSessionId, termId, section);
            return StatusCode(r.StatusCode, r);
        }

        // ==========================================================
        // OUTSTANDING SUBMISSIONS — who hasn't submitted yet
        // ==========================================================
        [HttpGet("outstanding")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Headmaster,Headmistress,SchoolAdmin")]
        public async Task<IActionResult> GetOutstanding(
            [FromQuery] Guid academicSessionId,
            [FromQuery] Guid termId,
            [FromQuery] Guid? classId = null)
        {
            var r = await _service.GetOutstandingSubmissionsAsync(
                academicSessionId, termId, classId);
            return StatusCode(r.StatusCode, r);
        }
    }
}