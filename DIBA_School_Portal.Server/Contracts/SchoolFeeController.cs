using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
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

        private string Ip =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // FEE CREATION METADATA — SuperAdmin, Principal, VicePrincipal, Accountant
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

        // LIST FEES (optionally filtered by session / term) — finance staff
        [HttpGet("list")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant")]
        public async Task<IActionResult> GetFees(
            [FromQuery] Guid? sessionId,
            [FromQuery] Guid? termId,
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _service.GetFeesAsync(sessionId, termId, includeDeleted);
            return StatusCode(result.StatusCode, result);
        }

        // GET ONE FEE — finance staff
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant")]
        public async Task<IActionResult> GetFeeById(Guid id)
        {
            var result = await _service.GetFeeByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        // SOFT DELETE FEE — SuperAdmin, Principal, Accountant
        [HttpDelete("soft-delete/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Accountant")]
        public async Task<IActionResult> SoftDeleteFee(Guid id)
        {
            var result = await _service.SoftDeleteFeeAsync(id, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // RESTORE FEE — SuperAdmin, Principal, Accountant
        [HttpPost("restore/{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Principal,Accountant")]
        public async Task<IActionResult> RestoreFee(Guid id)
        {
            var result = await _service.RestoreFeeAsync(id, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }
    }
}