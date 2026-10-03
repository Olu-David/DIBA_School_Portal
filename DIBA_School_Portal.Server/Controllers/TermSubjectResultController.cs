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

        // GET STUDENTS FOR SCORE ENTRY — teachers + admins
        [HttpGet("score-sheet")]
        [Authorize(Roles = "Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin")]
        public async Task<IActionResult> GetScoreSheet(
            [FromQuery] Guid classId,
            [FromQuery] Guid subjectId,
            [FromQuery] Guid termId,
            [FromQuery] Guid academicSessionId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _service.GetStudentsForScoreEntryAsync(
                classId, subjectId, termId, academicSessionId, pageNumber, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        // SAVE SCORES — teachers + form teachers only
        [HttpPost("save-scores")]
        [Authorize(Roles = "Teacher,FormTeacher")]
        public async Task<IActionResult> SaveScores([FromBody] ClassScoreSubmissionDto model)
        {
            var result = await _service.SaveAndComputeSubjectScoresAsync(model, CurrentUserId);
            return StatusCode(result.StatusCode, result);
        }

        // UPDATE INDIVIDUAL RESULT — override path (Principal, VP, SuperAdmin bypass 48h)
        [HttpPut("update-result/{subjectResultId:guid}")]
        [Authorize(Roles = "Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin")]
        public async Task<IActionResult> UpdateResult(
            Guid subjectResultId,
            [FromQuery] decimal test1,
            [FromQuery] decimal test2,
            [FromQuery] decimal exam)
        {
            var result = await _service.UpdateTermSubjectResultAsync(subjectResultId, test1, test2, exam, CurrentUserId);
            return StatusCode(result.StatusCode, result);
        }

        // GET REPORT CARD — everyone with a legitimate stake
        [HttpGet("report-card/{studentId}")]
        [Authorize(Roles = "Student,Parent,Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin")]
        public async Task<IActionResult> GetReportCard(string studentId, [FromQuery] Guid termId, [FromQuery] Guid academicSessionId)
        {
            var result = await _service.GetStudentReportCardAsync(studentId, termId, academicSessionId);
            return StatusCode(result.StatusCode, result);
        }
    }
}