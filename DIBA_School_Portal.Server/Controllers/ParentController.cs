using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ParentController : ControllerBase
    {
        private readonly IParentService _parentService;

        public ParentController(IParentService parentService)
        {
            _parentService = parentService;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Principal,Headmaster,Headmistress,VicePrincipal,SchoolAdmin,Registrar,Accountant")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? searchTerm = null)
        {
            var r = await _parentService.GetParentsAsync(pageNumber, pageSize, searchTerm);
            return StatusCode(r.StatusCode, r);
        }
    }
}