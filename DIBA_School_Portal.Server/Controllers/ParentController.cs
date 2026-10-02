using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Parent")]
    public class ParentController : ControllerBase
    {
        private readonly IParentService _parentService;

        public ParentController(IParentService parentService)
        {
            _parentService = parentService;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user ID claim.");

        // ---------------- Get children ----------------
        [HttpGet("children")]
        public async Task<IActionResult> GetChildren()
        {
            var result = await _parentService.GetMyChildrenAsync(CurrentUserId);
            return StatusCode(result.StatusCode, result);
        }

        // ---------------- Child dashboard ----------------
        [HttpGet("children/{studentProfileId:guid}/dashboard")]
        public async Task<IActionResult> GetChildDashboard(Guid studentProfileId)
        {
            var result = await _parentService.GetChildDashboardAsync(CurrentUserId, studentProfileId);
            return StatusCode(result.StatusCode, result);
        }

        // ---------------- Enrollments ----------------
        [HttpGet("children/{studentProfileId:guid}/enrollments")]
        public async Task<IActionResult> GetChildEnrollments(Guid studentProfileId)
        {
            var result = await _parentService.GetChildEnrollmentsAsync(CurrentUserId, studentProfileId);
            return StatusCode(result.StatusCode, result);
        }

        // ---------------- Bill breakdown ----------------
        [HttpGet("children/{studentProfileId:guid}/bill/{termId:guid}")]
        public async Task<IActionResult> GetChildBill(Guid studentProfileId, Guid termId)
        {
            var result = await _parentService.GetChildBillAsync(CurrentUserId, studentProfileId, termId);
            return StatusCode(result.StatusCode, result);
        }

        // ---------------- Payments ----------------
        [HttpGet("children/{studentProfileId:guid}/payments")]
        public async Task<IActionResult> GetChildPayments(Guid studentProfileId)
        {
            var result = await _parentService.GetChildPaymentsAsync(CurrentUserId, studentProfileId);
            return StatusCode(result.StatusCode, result);
        }

        // ---------------- Report card ----------------
        [HttpGet("children/{studentProfileId:guid}/report-card")]
        public async Task<IActionResult> GetChildReportCard(
            Guid studentProfileId,
            [FromQuery] Guid termId,
            [FromQuery] Guid academicSessionId)
        {
            var result = await _parentService.GetChildReportCardAsync(
                CurrentUserId, studentProfileId, termId, academicSessionId);
            return StatusCode(result.StatusCode, result);
        }
    }
}