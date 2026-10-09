using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnquiriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EnquiriesController(AppDbContext db)
        {
            _db = db;
        }

        // ---------------------------------------------------------------
        // PUBLIC — admission enquiry
        // ---------------------------------------------------------------
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Submit([FromBody] EnquiryRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid submission." });

            if (string.IsNullOrWhiteSpace(req.ParentName) ||
                string.IsNullOrWhiteSpace(req.ParentEmail) ||
                string.IsNullOrWhiteSpace(req.Message))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Name, email, and message are required."
                });
            }

            var enquiry = new Enquiry
            {
                Type = "Admission",
                ChildFirstName = req.ChildFirstName?.Trim(),
                ChildLastName = req.ChildLastName?.Trim(),
                ChildDateOfBirth = req.ChildDateOfBirth,
                ChildGender = req.ChildGender,
                Section = req.Section,
                PreviousSchool = req.PreviousSchool?.Trim(),
                PreferredStartTerm = req.PreferredStartTerm,
                ParentName = req.ParentName.Trim(),
                ParentEmail = req.ParentEmail.Trim().ToLowerInvariant(),
                ParentPhone = req.ParentPhone?.Trim() ?? string.Empty,
                Message = req.Message.Trim(),
                Status = "New",
                CreatedAt = DateTime.UtcNow,
            };

            _db.Enquiries.Add(enquiry);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, id = enquiry.Id });
        }

        // ---------------------------------------------------------------
        // PUBLIC — general contact form
        // ---------------------------------------------------------------
        [HttpPost("contact")]
        [AllowAnonymous]
        public async Task<IActionResult> Contact([FromBody] ContactRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid submission." });

            if (string.IsNullOrWhiteSpace(req.Name) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Message))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Name, email, and message are required."
                });
            }

            var enquiry = new Enquiry
            {
                Type = "Contact",
                ParentName = req.Name.Trim(),
                ParentEmail = req.Email.Trim().ToLowerInvariant(),
                ParentPhone = req.Phone?.Trim() ?? string.Empty,
                Subject = req.Subject?.Trim(),
                Message = req.Message.Trim(),
                Status = "New",
                CreatedAt = DateTime.UtcNow,
            };

            _db.Enquiries.Add(enquiry);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, id = enquiry.Id });
        }

        // ---------------------------------------------------------------
        // ADMIN — list enquiries (paginated, filterable)
        // ---------------------------------------------------------------
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        public async Task<IActionResult> List(
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 30)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, Math.Min(100, pageSize));

            var query = _db.Enquiries.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(e => e.Type == type);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(e => e.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(e =>
                    e.ParentName.ToLower().Contains(s) ||
                    e.ParentEmail.ToLower().Contains(s) ||
                    (e.ParentPhone != null && e.ParentPhone.Contains(s)) ||
                    (e.ChildFirstName != null && e.ChildFirstName.ToLower().Contains(s)) ||
                    (e.ChildLastName != null && e.ChildLastName.ToLower().Contains(s)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    items,
                    totalCount = total,
                    pageNumber,
                    pageSize,
                    totalPages = total > 0
                        ? (int)Math.Ceiling(total / (double)pageSize)
                        : 0,
                },
            });
        }

        // ---------------------------------------------------------------
        // ADMIN — update status / add notes
        // ---------------------------------------------------------------
        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "SuperAdmin,Principal,Registrar")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateEnquiryStatusRequest req)
        {
            var enquiry = await _db.Enquiries.FirstOrDefaultAsync(e => e.Id == id);
            if (enquiry == null)
                return NotFound(new { success = false, message = "Enquiry not found." });

            var allowed = new[] { "New", "Contacted", "Enrolled", "Lost" };
            if (!allowed.Contains(req.Status))
                return BadRequest(new { success = false, message = "Invalid status." });

            enquiry.Status = req.Status;
            if (!string.IsNullOrWhiteSpace(req.Notes)) enquiry.Notes = req.Notes;
            if (req.Status == "Contacted" && enquiry.ContactedAt == null)
                enquiry.ContactedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class UpdateEnquiryStatusRequest
    {
        public string Status { get; set; } = "New";
        public string? Notes { get; set; }
    }
}