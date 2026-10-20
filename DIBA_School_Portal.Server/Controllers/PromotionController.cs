using DIBA_School_Portal.Server.Contracts.PromotionDto;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Principal,SchoolAdmin")]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotion;

        public PromotionController(IPromotionService promotion) => _promotion = promotion;

        [HttpGet("preview")]
        public async Task<IActionResult> Preview([FromQuery] Guid? sessionId)
            => Ok(await _promotion.BuildPreviewAsync(sessionId));

        [HttpPost("{sessionId:guid}/execute")]
        public async Task<IActionResult> Execute(
            Guid sessionId, [FromBody] List<StudentStreamOverride> overrides)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _promotion.ExecuteAsync(sessionId, overrides ?? new(), userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{sessionId:guid}/history")]
        public async Task<IActionResult> History(Guid sessionId)
            => Ok(await _promotion.GetHistoryAsync(sessionId));
    }
}